using BuildingBlocks.Messaging.Contracts;
using TransactionService.Application.Models;

namespace TransactionService.Application.Interfaces
{
    public interface ITransactionProcessorService
    {
        Task ProcessAsync(TransactionCreated message, CancellationToken ct = default);
    }
}
