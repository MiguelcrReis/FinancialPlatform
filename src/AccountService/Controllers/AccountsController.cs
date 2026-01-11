using AccountService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace AccountService.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] ValidateAccountRequest request)
    {
        Log.Information(
            "Validating account {AccountId} for amount {Amount}",
            request.AccountId,
            request.Amount
        );

        // Simulação
        if (request.Amount > 1000)
        {
            Log.Warning("Insufficient funds");
            return BadRequest("Insufficient funds");
        }

        return Ok(new { Status = "Valid" });
    }
}
