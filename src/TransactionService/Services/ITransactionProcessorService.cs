using TransactionService.DTOs;
using TransactionService.Models;

namespace TransactionService.Services
{
    public interface ITransactionProcessorService
    {
        Task<Transaction> CreateAsync(CreateTransactionRequest request);
        Task<IEnumerable<Transaction>> GetAllAsync();
    }
}
