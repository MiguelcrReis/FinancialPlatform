namespace AccountService.DTOs;

public class ValidateAccountRequest
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}
