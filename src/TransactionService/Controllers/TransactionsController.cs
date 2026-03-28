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
    }
}
