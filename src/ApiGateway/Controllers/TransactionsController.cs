using ApiGateway.DTOs;
using ApiGateway.Services;
using BuildingBlocks.Correlation;
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
        private readonly ICorrelationContext _correlationContext;

        public TransactionsController(
            IMessagePublisher publisher,
            ICorrelationContext correlationContext)
        {
            _publisher = publisher;
            _correlationContext = correlationContext;
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

            var headers = !string.IsNullOrWhiteSpace(_correlationContext.CorrelationId)
                ? CorrelationIdHeaders.CreateRabbitMqHeaders(_correlationContext.CorrelationId)
                : null;

            await _publisher.PublishAsync("transaction.created", @event, headers);

            return Accepted();
        }
    }
}
