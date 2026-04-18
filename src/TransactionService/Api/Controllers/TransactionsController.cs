using Microsoft.AspNetCore.Mvc;
using TransactionService.DTOs;
using TransactionService.Application.Interfaces;

namespace TransactionService.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionQueryService _queryService;

    public TransactionsController(ITransactionQueryService queryService) => _queryService = queryService;

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionView>> GetById(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return BadRequest("Invalid id");
        var tx = await _queryService.GetByIdAsync(guid);
        if (tx == null) return NotFound();
        return tx;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionView>>> GetAll()
    {
        var list = await _queryService.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("by-account/{accountId}")]
    public async Task<ActionResult<IEnumerable<TransactionView>>> ByAccount(string accountId)
    {
        var list = await _queryService.GetByAccountAsync(accountId);
        return Ok(list);
    }
}
