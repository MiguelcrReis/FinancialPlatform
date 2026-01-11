using Microsoft.AspNetCore.Mvc;
using Serilog;
using TransactionService.DTOs;

namespace TransactionService.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        [HttpPost("process")]
        public IActionResult Process([FromBody] ProcessTransactionRequest request)
        {
            Log.Information(
                "Transaction received | AccountId={AccountId} Amount={Amount}",
                request.AccountId,
                request.Amount
            );

            if (request.Amount <= 0)
                return BadRequest("Invalid amount");

            return Ok(new
            {
                Status = "Processed",
                TransactionId = Guid.NewGuid()
            });
        }
    }
}
