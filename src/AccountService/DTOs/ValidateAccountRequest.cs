namespace AccountService.DTOs;

public class ValidateAccountRequest
{
    public string AccountId { get; set; } = null!;
    public decimal Amount { get; set; }
}   
