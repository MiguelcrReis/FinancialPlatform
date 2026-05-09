namespace TransactionService.Infrastructure.Settings
{
    public class RabbitMqSettings
    {
        public const string SectionName = "RabbitMq";

        public string HostName { get; set; } = "localhost";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string ExchangeName { get; set; } = "transactions-exchange";
        public string QueueName { get; set; } = "transactions";
        public string RoutingKey { get; set; } = "transaction.created";
        public string DeadLetterExchangeName { get; set; } = "transactions-dlx";
        public string DeadLetterQueueName { get; set; } = "transactions-dead-letter";
        public string DeadLetterRoutingKey { get; set; } = "transaction.failed";
        public int MaxRetryCount { get; set; } = 3;
    }
}
