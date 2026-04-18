namespace TransactionService.DTOs
{
    public record TransactionView(System.Guid Id, string FromAccount, string ToAccount, decimal Amount, System.DateTime CreatedAt, string Currency, string Description);
}
