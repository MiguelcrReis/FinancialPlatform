namespace AccountService.Models
{
    public class Account
    {
        public Guid Id { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
    }
}
