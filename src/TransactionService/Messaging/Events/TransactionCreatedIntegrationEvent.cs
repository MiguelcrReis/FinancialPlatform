using System;

namespace TransactionService.Messaging.Events
{
    // Integration event used by the Messaging layer.
    // Keeps messaging contracts isolated from API DTOs and application models.
    public record TransactionCreatedIntegrationEvent(
        Guid TransactionId,
        string FromAccount,
        string ToAccount,
        decimal Amount,
        string Currency,
        string? Description,
        DateTime CreatedAt
    );
}
