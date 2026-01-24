using Microsoft.AspNetCore.Mvc;
using Serilog;
using TransactionService.DTOs;
using TransactionService.Services;

namespace TransactionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionProcessorService _service;

        public TransactionsController(ITransactionProcessorService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTransactionRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            if (request.AccountFrom == request.AccountTo)
                return BadRequest("Origin and destination accounts cannot be the same.");

            var trx = await _service.CreateAsync(request);

            return Ok(new
            {
                trx.Id,
                trx.Status,
                trx.CreatedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }
    }
}
