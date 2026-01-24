namespace AccountService.DTOs
{
    public class ValidateAccountResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
