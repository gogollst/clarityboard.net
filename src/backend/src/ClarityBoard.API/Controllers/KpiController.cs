using ClarityBoard.Application.Features.KPI.Commands;
using ClarityBoard.Application.Features.KPI.DTOs;
using ClarityBoard.Application.Features.KPI.Queries;
using ClarityBoard.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class KpiController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IWorkingCapitalService _workingCapital;

    public KpiController(ISender mediator, IWorkingCapitalService workingCapital)
    {
        _mediator = mediator;
        _workingCapital = workingCapital;
    }

    // ======== Dashboard & History (existing) ========

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(KpiDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KpiDashboardDto>> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetKpiDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{kpiId}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<KpiSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KpiSnapshotDto>>> GetHistory(
        string kpiId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetKpiHistoryQuery
        {
            KpiId = kpiId,
            From = from,
            To = to,
        }, ct);
        return Ok(result);
    }

    // ======== KPI Definitions ========

    [HttpGet("definitions")]
    [ProducesResponseType(typeof(IReadOnlyList<KpiDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KpiDefinitionDto>>> GetDefinitions(
        [FromQuery] string? domain, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetKpiDefinitionsQuery { Domain = domain }, ct);
        return Ok(result);
    }

    // ======== KPI Drill-Down ========

    [HttpGet("{kpiId}/drill-down")]
    [ProducesResponseType(typeof(KpiDrillDownDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KpiDrillDownDto>> GetDrillDown(string kpiId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetKpiDrillDownQuery { KpiId = kpiId }, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    // ======== Working Capital ========

    [HttpGet("working-capital")]
    [ProducesResponseType(typeof(WorkingCapitalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkingCapitalDto>> GetWorkingCapital(
        [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        // EntityId comes from the current user claim; injected via ICurrentUser
        // but here we pull it from the JWT for the service call
        var entityIdClaim = User.FindFirst("entity_id")?.Value;
        if (!Guid.TryParse(entityIdClaim, out var entityId))
            return Unauthorized();

        var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _workingCapital.CalculateAsync(entityId, date, ct);

        return Ok(new WorkingCapitalDto
        {
            DSO = result.DSO,
            DIO = result.DIO,
            DPO = result.DPO,
            CCC = result.CCC,
            AgingBuckets = result.AgingBuckets,
        });
    }

    // ======== KPI Alerts ========

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<KpiAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KpiAlertDto>>> GetAlerts(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetKpiAlertsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("alerts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateAlert(
        [FromBody] CreateKpiAlertCommand command, CancellationToken ct)
    {
        var alertId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAlerts), new { }, alertId);
    }

    [HttpPut("alerts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAlert(
        Guid id, [FromBody] UpdateKpiAlertCommand command, CancellationToken ct)
    {
        // Bind the route id into the command
        var cmd = command with { Id = id };
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    [HttpDelete("alerts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAlert(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateKpiAlertCommand { Id = id }, ct);
        return NoContent();
    }

    // ======== Alert Events ========

    [HttpGet("alert-events")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertEventDto>>> GetAlertEvents(
        [FromQuery] string? status, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAlertEventsQuery { Status = status }, ct);
        return Ok(result);
    }

    [HttpPost("alert-events/{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcknowledgeAlertEvent(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new AcknowledgeAlertCommand { Id = id }, ct);
        return NoContent();
    }

    [HttpPost("alert-events/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResolveAlertEvent(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ResolveAlertCommand { Id = id }, ct);
        return NoContent();
    }
}
