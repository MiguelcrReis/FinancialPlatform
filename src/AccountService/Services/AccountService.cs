using AccountService.DTOs;
using AccountService.Models;

namespace AccountService.Services
{
    public class AccountService : IAccountService
    {
        // Mock inicial – depois trocamos por MongoDB
        private static readonly List<Account> Accounts = new()
        {
            new Account
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Balance = 1000,
                IsActive = true
            },
            new Account
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Balance = 50,
                IsActive = true
            },
            new Account
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Balance = 0,
                IsActive = false
            }
        };

        public ValidateAccountResponse Validate(ValidateAccountRequest request)
        {
            var acc = Accounts.FirstOrDefault(a => a.Id == request.AccountId);

            if (acc == null)
                return new ValidateAccountResponse
                {
                    IsValid = false,
                    Message = "Account not found."
                };

            if (!acc.IsActive)
                return new ValidateAccountResponse
                {
                    IsValid = false,
                    Message = "Account is disabled."
                };

            if (acc.Balance < request.Amount)
                return new ValidateAccountResponse
                {
                    IsValid = false,
                    Message = "Insufficient balance."
                };

            return new ValidateAccountResponse
            {
                IsValid = true,
                Message = "Account validated successfully."
            };
        }
    }
}
