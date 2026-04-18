using TransactionService.DTOs;

namespace TransactionService.Application.Interfaces
{
    public interface ITransactionQueryService
    {
        Task<TransactionView?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<TransactionView>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<TransactionView>> GetByAccountAsync(string accountId, CancellationToken ct = default);
    }
}
