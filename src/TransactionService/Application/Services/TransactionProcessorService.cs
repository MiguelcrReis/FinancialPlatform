using BuildingBlocks.Messaging.Contracts;
using TransactionService.Application.Interfaces;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Domain;

namespace TransactionService.Application.Services
{
    public class TransactionProcessorService : ITransactionProcessorService
    {
        private readonly ITransactionRepository _repository;
        private readonly ILogger<TransactionProcessorService> _logger;

        public TransactionProcessorService(
            ITransactionRepository repository,
            ILogger<TransactionProcessorService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task ProcessAsync(TransactionCreatedEvent message, CancellationToken ct = default)
        {
            // map integration event to domain
            var tx = new Transaction
            {
                Id = message.TransactionId,
                ExternalId = message.TransactionId.ToString(),
                FromAccount = message.FromAccount,
                ToAccount = message.ToAccount,
                Amount = message.Amount,
                Currency = message.Currency,
                Description = message.Description ?? string.Empty,
                CreatedAt = message.CreatedAt == default ? DateTime.UtcNow : message.CreatedAt
            };

            await _repository.AddAsync(tx, ct);

            _logger.LogInformation("Processed transaction {TransactionId}", tx.Id);
        }
    }
}
