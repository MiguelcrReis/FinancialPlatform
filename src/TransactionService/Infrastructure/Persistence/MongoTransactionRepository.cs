using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TransactionService.Domain;
using TransactionService.Infrastructure.Settings;

namespace TransactionService.Infrastructure.Persistence
{
    public class MongoTransactionRepository : ITransactionRepository
    {
        private readonly IMongoCollection<Transaction> _collection;

        public MongoTransactionRepository(
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.Database);
            _collection = database.GetCollection<Transaction>(settings.Collection);

            CreateIndexes();
        }

        public Task AddAsync(Transaction transaction, CancellationToken ct = default)
        {
            return InsertIgnoringDuplicatesAsync(transaction, ct);
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default)
        {
            return await _collection
                .Find(Builders<Transaction>.Filter.Empty)
                .SortByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _collection
                .Find(t => t.Id == id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<Transaction>> GetByAccountAsync(string accountId, CancellationToken ct = default)
        {
            var filter = Builders<Transaction>.Filter.Or(
                Builders<Transaction>.Filter.Eq(t => t.FromAccount, accountId),
                Builders<Transaction>.Filter.Eq(t => t.ToAccount, accountId));

            return await _collection
                .Find(filter)
                .SortByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        private async Task InsertIgnoringDuplicatesAsync(Transaction transaction, CancellationToken ct)
        {
            try
            {
                await _collection.InsertOneAsync(transaction, cancellationToken: ct);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Duplicate ExternalId means the event was already processed.
            }
        }

        private void CreateIndexes()
        {
            var externalIdIndex = new CreateIndexModel<Transaction>(
                Builders<Transaction>.IndexKeys.Ascending(t => t.ExternalId),
                new CreateIndexOptions<Transaction>
                {
                    Name = "ux_transactions_external_id",
                    Unique = true
                });

            var accountIndex = new CreateIndexModel<Transaction>(
                Builders<Transaction>.IndexKeys
                    .Ascending(t => t.FromAccount)
                    .Ascending(t => t.ToAccount),
                new CreateIndexOptions { Name = "ix_transactions_accounts" });

            _collection.Indexes.CreateMany(new[] { externalIdIndex, accountIndex });
        }
    }
}
