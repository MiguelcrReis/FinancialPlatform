namespace ApiGateway.DTOs
{
    public class CreateTransactionRequest
    {
        public string AccountId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BRL";
        public string Description { get; set; } = string.Empty;
    }
}
