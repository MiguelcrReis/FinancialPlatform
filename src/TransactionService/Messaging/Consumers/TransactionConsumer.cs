using BuildingBlocks.Messaging.Contracts;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TransactionService.Application.Interfaces;

namespace TransactionService.Messaging.Consumers
{
    public class TransactionConsumer : BackgroundService
    {
        private readonly IModel _channel;
        private readonly IServiceProvider _services;
        private const string ExchangeName = "transactions-exchange";
        private const string QueueName = "transactions";

        public TransactionConsumer(IConnection connection, IServiceProvider services)
        {
            _services = services;
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, "transaction.created");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                TransactionCreatedEvent? message = null;
                try
                {
                    message = JsonSerializer.Deserialize<TransactionCreatedEvent>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to deserialize message: {ex.Message}");
                }

                if (message != null)
                {
                    try
                    {
                        using var scope = _services.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessorService>();

                        await processor.ProcessAsync(message, stoppingToken);

                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Processing failed: {ex.Message}");
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                }
                else
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(QueueName, autoAck: false, consumer);
            return Task.CompletedTask;
        }
    }
}
