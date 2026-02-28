using ClarityBoard.Application.Features.Scenarios.Commands;
using ClarityBoard.Application.Features.Scenarios.DTOs;
using ClarityBoard.Application.Features.Scenarios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ScenarioController : ControllerBase
{
    private readonly ISender _mediator;

    public ScenarioController(ISender mediator) => _mediator = mediator;

    // ── List Scenarios ──

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ScenarioListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScenarioListDto>>> GetScenarios(
        [FromQuery] Guid entityId,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetScenariosQuery
        {
            EntityId = entityId,
            Type = type,
            Status = status,
        }, ct);
        return Ok(result);
    }

    // ── Get Scenario Detail ──

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScenarioDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScenarioDetailDto>> GetScenarioDetail(
        Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetScenarioDetailQuery { ScenarioId = id }, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    // ── Create Scenario ──

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateScenario(
        [FromBody] CreateScenarioCommand command, CancellationToken ct)
    {
        var scenarioId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetScenarioDetail), new { id = scenarioId }, scenarioId);
    }

    // ── Run Scenario ──

    [HttpPost("{id:guid}/run")]
    [ProducesResponseType(typeof(IReadOnlyList<ScenarioResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ScenarioResultDto>>> RunScenario(
        Guid id, CancellationToken ct)
    {
        var results = await _mediator.Send(new RunScenarioCommand { ScenarioId = id }, ct);
        return Ok(results);
    }
}
