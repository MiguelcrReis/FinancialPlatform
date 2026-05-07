namespace TransactionService.Infrastructure.Settings
{
    public class MongoDbSettings
    {
        public const string SectionName = "MongoDb";

        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string Database { get; set; } = "financialdb";
        public string Collection { get; set; } = "transactions";
    }
}
