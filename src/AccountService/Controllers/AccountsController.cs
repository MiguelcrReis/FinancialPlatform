using AccountService.DTOs;
using AccountService.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace AccountService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Validates if an account exists, is active, and has enough balance.
    /// </summary>
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateAccountRequest request)
    {
        var response = _accountService.Validate(request);

        if (!response.IsValid)
            return BadRequest(response);

        return Ok(response);
    }
}
