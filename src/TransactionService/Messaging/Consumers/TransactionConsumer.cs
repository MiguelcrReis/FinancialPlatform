using System.Text;
using System.Text.Json;
using BuildingBlocks.Messaging.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransactionService.Application.Interfaces;
using TransactionService.Infrastructure.Settings;

namespace TransactionService.Messaging.Consumers
{
    public class TransactionConsumer : BackgroundService
    {
        private const string RetryCountHeader = "x-retry-count";
        private const string ErrorReasonHeader = "x-error-reason";
        private const string OriginalRoutingKeyHeader = "x-original-routing-key";

        private readonly IModel _channel;
        private readonly IServiceProvider _services;
        private readonly RabbitMqSettings _settings;
        private readonly ILogger<TransactionConsumer> _logger;
        private readonly object _channelLock = new();

        public TransactionConsumer(
            IConnection connection,
            IServiceProvider services,
            IOptions<RabbitMqSettings> options,
            ILogger<TransactionConsumer> logger)
        {
            _services = services;
            _settings = options.Value;
            _logger = logger;
            _channel = connection.CreateModel();

            DeclareTopology();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                TransactionCreatedEvent? message;

                try
                {
                    message = JsonSerializer.Deserialize<TransactionCreatedEvent>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize transaction message. Sending to DLQ.");
                    PublishToDeadLetter(ea, "deserialization_failed");
                    Ack(ea.DeliveryTag);
                    return;
                }

                if (message is null)
                {
                    _logger.LogWarning("Transaction message deserialized to null. Sending to DLQ.");
                    PublishToDeadLetter(ea, "message_null");
                    Ack(ea.DeliveryTag);
                    return;
                }

                try
                {
                    using var scope = _services.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessorService>();

                    await processor.ProcessAsync(message, stoppingToken);

                    Ack(ea.DeliveryTag);
                    _logger.LogInformation("Processed transaction message {TransactionId}", message.TransactionId);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(
                        "Transaction consumer is stopping before message {TransactionId} completed.",
                        message.TransactionId);
                }
                catch (Exception ex)
                {
                    var nextRetryCount = GetRetryCount(ea.BasicProperties) + 1;

                    if (nextRetryCount <= _settings.MaxRetryCount)
                    {
                        _logger.LogWarning(
                            ex,
                            "Processing failed for transaction message {TransactionId}. Retrying attempt {RetryCount}/{MaxRetryCount}.",
                            message.TransactionId,
                            nextRetryCount,
                            _settings.MaxRetryCount);

                        PublishRetry(ea, nextRetryCount);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Processing failed for transaction message {TransactionId} after {MaxRetryCount} retries. Sending to DLQ.",
                            message.TransactionId,
                            _settings.MaxRetryCount);

                        PublishToDeadLetter(ea, "max_retries_exceeded");
                    }

                    Ack(ea.DeliveryTag);
                }
            };

            _channel.BasicConsume(_settings.QueueName, autoAck: false, consumer);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Dispose();
            base.Dispose();
        }

        private void DeclareTopology()
        {
            _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Direct, durable: true);
            _channel.ExchangeDeclare(_settings.DeadLetterExchangeName, ExchangeType.Direct, durable: true);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(_settings.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);

            _channel.QueueBind(_settings.QueueName, _settings.ExchangeName, _settings.RoutingKey);
            _channel.QueueBind(
                _settings.DeadLetterQueueName,
                _settings.DeadLetterExchangeName,
                _settings.DeadLetterRoutingKey);
        }

        private void PublishRetry(BasicDeliverEventArgs ea, int retryCount)
        {
            var properties = CreateForwardProperties(ea.BasicProperties);
            properties.Headers ??= new Dictionary<string, object>();
            properties.Headers[RetryCountHeader] = retryCount;

            Publish(
                _settings.ExchangeName,
                _settings.RoutingKey,
                properties,
                ea.Body);
        }

        private void PublishToDeadLetter(BasicDeliverEventArgs ea, string reason)
        {
            var properties = CreateForwardProperties(ea.BasicProperties);
            properties.Headers ??= new Dictionary<string, object>();
            properties.Headers[RetryCountHeader] = GetRetryCount(ea.BasicProperties);
            properties.Headers[ErrorReasonHeader] = reason;
            properties.Headers[OriginalRoutingKeyHeader] = ea.RoutingKey;

            Publish(
                _settings.DeadLetterExchangeName,
                _settings.DeadLetterRoutingKey,
                properties,
                ea.Body);
        }

        private IBasicProperties CreateForwardProperties(IBasicProperties? source)
        {
            IBasicProperties properties;
            lock (_channelLock)
            {
                properties = _channel.CreateBasicProperties();
            }

            properties.Persistent = true;
            properties.ContentType = source?.ContentType ?? "application/json";
            properties.CorrelationId = source?.CorrelationId;
            properties.MessageId = source?.MessageId;
            properties.Type = source?.Type;
            properties.Headers = source?.Headers is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(source.Headers);

            return properties;
        }

        private void Publish(
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            lock (_channelLock)
            {
                _channel.BasicPublish(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);
            }
        }

        private void Ack(ulong deliveryTag)
        {
            lock (_channelLock)
            {
                _channel.BasicAck(deliveryTag, multiple: false);
            }
        }

        private static int GetRetryCount(IBasicProperties? properties)
        {
            if (properties?.Headers is null ||
                !properties.Headers.TryGetValue(RetryCountHeader, out var value))
            {
                return 0;
            }

            return value switch
            {
                byte retryCount => retryCount,
                short retryCount => retryCount,
                int retryCount => retryCount,
                long retryCount => Convert.ToInt32(retryCount),
                byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var retryCount) => retryCount,
                string text when int.TryParse(text, out var retryCount) => retryCount,
                _ => 0
            };
        }
    }
}
