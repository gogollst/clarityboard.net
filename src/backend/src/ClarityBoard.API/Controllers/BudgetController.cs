using ClarityBoard.Application.Features.Budget.Commands;
using ClarityBoard.Application.Features.Budget.DTOs;
using ClarityBoard.Application.Features.Budget.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BudgetController : ControllerBase
{
    private readonly ISender _mediator;

    public BudgetController(ISender mediator) => _mediator = mediator;

    // ── List Budgets ──

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BudgetListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BudgetListDto>>> GetBudgets(
        [FromQuery] Guid entityId,
        [FromQuery] short? fiscalYear = null,
        [FromQuery] string? status = null,
        [FromQuery] string? department = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBudgetsQuery
        {
            EntityId = entityId,
            FiscalYear = fiscalYear,
            Status = status,
            Department = department,
        }, ct);
        return Ok(result);
    }

    // ── Get Budget Detail ──

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDetailDto>> GetBudgetDetail(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBudgetDetailQuery { BudgetId = id }, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    // ── Create Budget ──

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateBudget(
        [FromBody] CreateBudgetCommand command, CancellationToken ct)
    {
        var budgetId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetBudgetDetail), new { id = budgetId }, budgetId);
    }

    // ── Create Budget Revision ──

    [HttpPost("{id:guid}/revisions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateRevision(
        Guid id, [FromBody] CreateBudgetRevisionCommand command, CancellationToken ct)
    {
        var cmd = command with { BudgetId = id };
        var revisionId = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetBudgetDetail), new { id }, revisionId);
    }

    // ── Approve Budget ──

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveBudget(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ApproveBudgetCommand { BudgetId = id }, ct);
        return NoContent();
    }

    // ── Plan vs Actual Comparison ──

    [HttpGet("{id:guid}/plan-vs-actual")]
    [ProducesResponseType(typeof(PlanVsActualDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanVsActualDto>> GetPlanVsActual(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPlanVsActualQuery { BudgetId = id }, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
}
