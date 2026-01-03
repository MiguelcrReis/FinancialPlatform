using ApiGateway.DTOs;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        [HttpPost]
        public IActionResult Create([FromBody] CreateTransactionRequest request)
        {
            Log.Information(
                "Received transaction request | AccountId={AccountId} Amount={Amount} Currency={Currency}",
                request.AccountId,
                request.Amount,
                request.Currency
            );

            return Accepted(new
            {
                Message = "Transaction accepted for processing",
                CorrelationId = HttpContext.TraceIdentifier
            });
        }
    }
}
