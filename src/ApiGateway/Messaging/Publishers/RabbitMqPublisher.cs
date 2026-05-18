using System.Text;
using System.Text.Json;
using BuildingBlocks.Correlation;
using BuildingBlocks.Messaging.Interfaces;
using RabbitMQ.Client;

namespace ApiGateway.Messaging.Publishers
{
    public class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private const string ExchangeName = "transactions-exchange";
        private const int DefaultConnectionRetryCount = 10;
        private static readonly TimeSpan DefaultConnectionRetryDelay = TimeSpan.FromSeconds(2);

        public RabbitMqPublisher(
            IConfiguration configuration,
            ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;
            var cfg = configuration.GetSection("RabbitMq");
            var host = cfg.GetValue<string>("HostName") ?? "localhost";
            var user = cfg.GetValue<string>("UserName");
            var pass = cfg.GetValue<string>("Password");
            var retryCount = cfg.GetValue<int?>("ConnectionRetryCount") ?? DefaultConnectionRetryCount;
            var retryDelaySeconds = cfg.GetValue<int?>("ConnectionRetryDelaySeconds")
                ?? (int)DefaultConnectionRetryDelay.TotalSeconds;

            var factory = new ConnectionFactory()
            {
                HostName = host,
                DispatchConsumersAsync = true
            };

            if (!string.IsNullOrEmpty(user)) factory.UserName = user;
            if (!string.IsNullOrEmpty(pass)) factory.Password = pass;

            _connection = CreateConnectionWithRetry(
                factory,
                logger,
                retryCount,
                TimeSpan.FromSeconds(retryDelaySeconds));
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true
            );
        }

        public Task PublishAsync<T>(
            string routingKey,
            T message,
            IReadOnlyDictionary<string, object>? headers = null)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString("D");

            if (headers is not null)
            {
                properties.Headers = new Dictionary<string, object>(headers);

                var correlationId = CorrelationIdHeaders.ReadRabbitMqCorrelationId(properties.Headers);
                if (correlationId is not null)
                {
                    properties.Headers[CorrelationIdConstants.RabbitMqHeaderName] = correlationId;
                    properties.CorrelationId = correlationId;
                }
            }

            var publishedCorrelationId = CorrelationIdHeaders.ReadRabbitMqCorrelationId(properties.Headers);

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "Published RabbitMQ message with routing key {RoutingKey} and correlation id {CorrelationId}",
                routingKey,
                publishedCorrelationId);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }

        private static IConnection CreateConnectionWithRetry(
            ConnectionFactory factory,
            ILogger logger,
            int maxAttempts,
            TimeSpan retryDelay)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return factory.CreateConnection();
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(
                        ex,
                        "RabbitMQ connection failed. Attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds.",
                        attempt,
                        maxAttempts,
                        retryDelay.TotalSeconds);

                    Thread.Sleep(retryDelay);
                }
            }

            return factory.CreateConnection();
        }
    }
}
