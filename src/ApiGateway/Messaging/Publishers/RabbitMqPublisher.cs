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
        private const int DefaultConnectionRetryCount = 10;
        private static readonly TimeSpan DefaultConnectionRetryDelay = TimeSpan.FromSeconds(2);

        public RabbitMqPublisher(
            IConfiguration configuration,
            ILogger<RabbitMqPublisher> logger)
        {
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

        public Task PublishAsync<T>(string routingKey, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

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
