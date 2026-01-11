namespace TransactionService.DTOs
{
    public class ProcessTransactionRequest
    {
        public string AccountId { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
