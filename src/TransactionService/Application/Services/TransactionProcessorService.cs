using BuildingBlocks.Messaging.Contracts;
using TransactionService.Application.Interfaces;
using TransactionService.Application.Models;
using TransactionService.Infrastructure.Persistence;
using TransactionService.Domain;

namespace TransactionService.Application.Services
{
    public class TransactionProcessorService : ITransactionProcessorService
    {
        private readonly ITransactionRepository _repository;

        public TransactionProcessorService(ITransactionRepository repository)
        {
            _repository = repository;
        }

        public async Task ProcessAsync(TransactionCreated message, CancellationToken ct = default)
        {
            // map application model to domain
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

            // TODO: publish further events / side-effects
            Console.WriteLine($"Processed transaction {tx.Id}");
        }
    }
}
