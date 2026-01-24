using AccountService.DTOs;

namespace AccountService.Services
{
    public interface IAccountService
    {
        ValidateAccountResponse Validate(ValidateAccountRequest request);
    }
}
