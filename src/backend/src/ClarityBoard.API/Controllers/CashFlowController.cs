using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.CashFlow.Commands;
using ClarityBoard.Application.Features.CashFlow.DTOs;
using ClarityBoard.Application.Features.CashFlow.Queries;
using ClarityBoard.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CashFlowController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IWorkingCapitalService _workingCapital;

    public CashFlowController(ISender mediator, IWorkingCapitalService workingCapital)
    {
        _mediator = mediator;
        _workingCapital = workingCapital;
    }

    // ── Overview (Operating / Investment / Financing breakdown) ──

    [HttpGet("overview")]
    [ProducesResponseType(typeof(CashFlowOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CashFlowOverviewDto>> GetOverview(
        [FromQuery] Guid entityId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCashFlowOverviewQuery
        {
            EntityId = entityId,
            From = from,
            To = to,
        }, ct);
        return Ok(result);
    }

    // ── 13-Week Forecast ──

    [HttpGet("forecast")]
    [ProducesResponseType(typeof(CashFlowForecastDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CashFlowForecastDto>> GetForecast(
        [FromQuery] Guid entityId,
        [FromQuery] DateOnly? startDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCashFlowForecastQuery
        {
            EntityId = entityId,
            StartDate = startDate,
        }, ct);
        return Ok(result);
    }

    // ── Working Capital (DSO / DIO / DPO / CCC) ──

    [HttpGet("working-capital")]
    [ProducesResponseType(typeof(WorkingCapitalSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkingCapitalSummaryDto>> GetWorkingCapital(
        [FromQuery] Guid entityId,
        [FromQuery] DateOnly? asOf = null,
        CancellationToken ct = default)
    {
        var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _workingCapital.CalculateAsync(entityId, date, ct);

        return Ok(new WorkingCapitalSummaryDto
        {
            DSO = result.DSO,
            DIO = result.DIO,
            DPO = result.DPO,
            CCC = result.CCC,
            AgingBuckets = result.AgingBuckets.Select(b => new AgingBucketDto
            {
                Label = b.Label,
                MinDays = b.MinDays,
                MaxDays = b.MaxDays,
                Amount = b.Amount,
            }).ToList(),
        });
    }

    // ── Create Manual Cash Flow Entry ──

    [HttpPost("entries")]
    [ProducesResponseType(typeof(CashFlowEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CashFlowEntryDto>> CreateEntry(
        [FromBody] CreateCashFlowEntryCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetEntries), new { entityId = result.EntityId }, result);
    }

    // ── List Entries with Filters ──

    [HttpGet("entries")]
    [ProducesResponseType(typeof(PagedResult<CashFlowEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CashFlowEntryDto>>> GetEntries(
        [FromQuery] Guid entityId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? category = null,
        [FromQuery] string? certainty = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCashFlowEntriesQuery
        {
            EntityId = entityId,
            From = from,
            To = to,
            Category = category,
            Certainty = certainty,
            Page = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }
}
