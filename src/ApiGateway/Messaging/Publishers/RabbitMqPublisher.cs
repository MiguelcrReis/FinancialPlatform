using BuildingBlocks.Messaging.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Messaging.Publishers
{
    public class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string ExchangeName = "transactions-exchange";

        public RabbitMqPublisher()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // 🔥 Garante que a exchange existe
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true
            );
        }

        public Task PublishAsync<T>(string routingKey, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // 🔥 mensagem não se perde

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
