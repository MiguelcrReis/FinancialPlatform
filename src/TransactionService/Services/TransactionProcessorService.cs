using BuildingBlocks.Messaging.Contracts;
using TransactionService.DTOs;
using TransactionService.Models;

namespace TransactionService.Services
{
    public class TransactionProcessorService : ITransactionProcessorService
    {
        public Task ProcessAsync(TransactionCreatedEvent message)
        {
            Console.WriteLine("=== PROCESSANDO TRANSAÇÃO ===");

            Console.WriteLine($"Id: {message.TransactionId}");
            Console.WriteLine($"From: {message.FromAccount}");
            Console.WriteLine($"To: {message.ToAccount}");
            Console.WriteLine($"Amount: {message.Amount}");
            Console.WriteLine($"Currency: {message.Currency}");

            // ToDo: salvar no banco

            return Task.CompletedTask;
        }
    }
}
