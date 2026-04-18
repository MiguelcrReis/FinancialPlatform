using TransactionService.Application.Interfaces;
using TransactionService.DTOs;
using TransactionService.Infrastructure.Persistence;

namespace TransactionService.Application.Services
{
    public class TransactionQueryService : ITransactionQueryService
    {
        private readonly ITransactionRepository _repository;

        public TransactionQueryService(ITransactionRepository repository)
        {
            _repository = repository;
        }

        public async Task<TransactionView?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var tx = await _repository.GetByIdAsync(id, ct);
            if (tx == null) return null;
            return new TransactionView(tx.Id, tx.FromAccount, tx.ToAccount, tx.Amount, tx.CreatedAt, tx.Currency, tx.Description);
        }

        public async Task<IEnumerable<TransactionView>> GetAllAsync(CancellationToken ct = default)
        {
            var list = await _repository.GetAllAsync(ct);
            return list.Select(t => new TransactionView(t.Id, t.FromAccount, t.ToAccount, t.Amount, t.CreatedAt, t.Currency, t.Description));
        }

        public async Task<IEnumerable<TransactionView>> GetByAccountAsync(string accountId, CancellationToken ct = default)
        {
            var list = await _repository.GetByAccountAsync(accountId, ct);
            return list.Select(t => new TransactionView(t.Id, t.FromAccount, t.ToAccount, t.Amount, t.CreatedAt, t.Currency, t.Description));
        }
    }
}
