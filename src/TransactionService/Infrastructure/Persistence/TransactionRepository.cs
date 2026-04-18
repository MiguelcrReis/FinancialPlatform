using System.Collections.Concurrent;
using TransactionService.Domain;

namespace TransactionService.Infrastructure.Persistence
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ConcurrentDictionary<Guid, Transaction> _store = new();
        // map ExternalId -> TransactionId for quick idempotency checks
        private readonly ConcurrentDictionary<string, Guid> _externalIndex = new();

        public Task AddAsync(Transaction transaction, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(transaction.ExternalId))
            {
                // If external id already processed, skip
                if (_externalIndex.ContainsKey(transaction.ExternalId))
                    return Task.CompletedTask;
            }

            // ensure unique id
            _store[transaction.Id] = transaction;

            if (!string.IsNullOrEmpty(transaction.ExternalId))
            {
                _externalIndex.TryAdd(transaction.ExternalId, transaction.Id);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<IEnumerable<Transaction>>(_store.Values);

        public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _store.TryGetValue(id, out var tx);
            return Task.FromResult(tx);
        }

        public Task<IEnumerable<Transaction>> GetByAccountAsync(string accountId, CancellationToken ct = default)
        {
            var list = _store.Values.Where(t => t.FromAccount == accountId || t.ToAccount == accountId);
            return Task.FromResult<IEnumerable<Transaction>>(list);
        }
    }
}
