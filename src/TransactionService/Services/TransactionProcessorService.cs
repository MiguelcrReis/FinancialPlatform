using TransactionService.DTOs;
using TransactionService.Models;

namespace TransactionService.Services
{
    public class TransactionProcessorService : ITransactionProcessorService
    {
        private static readonly List<Transaction> _transactions = new();

        public Task<Transaction> CreateAsync(CreateTransactionRequest request)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                AccountFrom = request.AccountFrom,
                AccountTo = request.AccountTo,
                Amount = request.Amount,
                Type = request.Type,
                CreatedAt = DateTime.UtcNow,
                Status = "APPROVED"
            };

            _transactions.Add(transaction);

            return Task.FromResult(transaction);
        }

        public Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return Task.FromResult(_transactions.AsEnumerable());
        }
    }
}
