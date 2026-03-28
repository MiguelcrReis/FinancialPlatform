using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.Contracts
{
    public record TransactionCreatedEvent(
        Guid TransactionId,
        string FromAccount,
        string ToAccount,
        decimal Amount,
        string Currency,
        string Description,
        DateTime CreatedAt
    );
}
