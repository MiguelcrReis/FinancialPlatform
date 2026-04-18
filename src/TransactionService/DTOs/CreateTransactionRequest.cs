namespace TransactionService.DTOs
{
    public class CreateTransactionRequest
    {
        public string AccountFrom { get; set; } = null!;
        public string AccountTo { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Type { get; set; } = ""; // PIX, TED, etc.
    }
}
