using BuildingBlocks.Messaging.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TransactionService.Services;

namespace TransactionService.Consumers
{
    public class TransactionConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private IConnection _connection;
        private IModel _channel;

        private const string ExchangeName = "transactions-exchange";
        private const string QueueName = "transactions";

        public TransactionConsumer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);

            _channel.QueueDeclare(QueueName, true, false, false);

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: "transaction.created"
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                using var scope = _scopeFactory.CreateScope();

                var service = scope.ServiceProvider
                    .GetRequiredService<ITransactionProcessorService>();

                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    var message = JsonSerializer.Deserialize<TransactionCreatedEvent>(json);

                    await service.ProcessAsync(message);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");

                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(QueueName, false, consumer);

            return Task.CompletedTask;
        }
    }
}
