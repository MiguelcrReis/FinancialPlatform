namespace TransactionService.DTOs
{
    public class TransactionResponse
    {
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
