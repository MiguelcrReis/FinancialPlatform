namespace TransactionService.Models
{
    public class Transaction
    {
        public string Id { get; set; }
        public string AccountFrom { get; set; }
        public string AccountTo { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}
