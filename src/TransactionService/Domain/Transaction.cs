namespace TransactionService.Domain
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        // ExternalId is used to ensure idempotency when processing integration events
        public string ExternalId { get; set; } = string.Empty;
        public string FromAccount { get; set; } = null!;
        public string ToAccount { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
