namespace TransactionService.DTOs
{
    public class CreateTransactionRequest
    {
        public string AccountFrom { get; set; }
        public string AccountTo { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } // PIX, TED, etc.
    }
}
