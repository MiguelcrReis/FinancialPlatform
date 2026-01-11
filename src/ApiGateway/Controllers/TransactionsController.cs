using ApiGateway.DTOs;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create(
            CreateTransactionRequest request,
            AccountServiceClient accountClient,
            TransactionServiceClient transactionClient)
        {
            var isValid = await accountClient.ValidateAsync(request);

            if (!isValid)
                return BadRequest("Account validation failed");

            var response = await transactionClient.CreateAsync(request);

            return StatusCode((int)response.StatusCode);
        }
    }
}
