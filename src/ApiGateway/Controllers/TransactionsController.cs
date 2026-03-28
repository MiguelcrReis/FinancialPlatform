using ApiGateway.DTOs;
using ApiGateway.Services;
using BuildingBlocks.Messaging.Contracts;
using BuildingBlocks.Messaging.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMessagePublisher _publisher;

        public TransactionsController(IMessagePublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateTransactionRequest request,
            AccountServiceClient accountClient)
        {
            var isValid = await accountClient.ValidateAsync(request, HttpContext.RequestAborted);

            if (!isValid)
                return BadRequest("Account validation failed");

            var @event = new TransactionCreatedEvent(
                Guid.NewGuid(),
                request.FromAccount,
                request.ToAccount,
                request.Amount,
                request.Currency,
                request.Description,
                DateTime.UtcNow
            );

            await _publisher.PublishAsync("transaction.created", @event);

            return Accepted();
        }
    }
}
