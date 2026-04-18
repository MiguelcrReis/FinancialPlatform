using System;

namespace TransactionService.Application.Models
{
    // Application-level model representing a transaction creation request originating from messaging.
    public record TransactionCreated(Guid TransactionId, string FromAccount, string ToAccount, decimal Amount, string Currency, string? Description, DateTime CreatedAt);
}
