using BuildingBlocks.Messaging.Contracts;

namespace TransactionService.Services
{
    public interface ITransactionProcessorService
    {
        Task ProcessAsync(TransactionCreatedEvent message);
    }
}