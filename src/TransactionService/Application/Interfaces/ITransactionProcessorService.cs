using BuildingBlocks.Messaging.Contracts;

namespace TransactionService.Application.Interfaces
{
    public interface ITransactionProcessorService
    {
        Task ProcessAsync(TransactionCreatedEvent message, CancellationToken ct = default);
    }
}
