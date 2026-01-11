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
        public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, [FromServices] TransactionServiceClient client)
        {
            var response = await client.CreateAsync(request);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
    }
}
