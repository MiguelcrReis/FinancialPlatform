using TransactionService.Domain;

namespace TransactionService.Infrastructure.Persistence
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction, CancellationToken ct = default);
        Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Transaction>> GetByAccountAsync(string accountId, CancellationToken ct = default);
        Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default);
    }
}
