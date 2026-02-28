using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Integration.Commands;
using ClarityBoard.Application.Features.Integration.DTOs;
using ClarityBoard.Application.Features.Integration.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WebhookConfigController : ControllerBase
{
    private readonly ISender _mediator;

    public WebhookConfigController(ISender mediator)
    {
        _mediator = mediator;
    }

    // ── Webhook Configs ───────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WebhookConfigDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WebhookConfigDto>>> GetWebhookConfigs(
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWebhookConfigsQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateWebhookConfig(
        [FromBody] CreateWebhookConfigCommand command, CancellationToken ct)
    {
        var configId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetWebhookConfigs), new { }, configId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWebhookConfig(
        Guid id, [FromBody] UpdateWebhookConfigCommand command, CancellationToken ct)
    {
        var cmd = command with { Id = id };
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    // ── Mapping Rules ─────────────────────────────────────────────────────

    [HttpGet("mapping-rules")]
    [ProducesResponseType(typeof(IReadOnlyList<MappingRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MappingRuleDto>>> GetMappingRules(
        [FromQuery] string? sourceType, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMappingRulesQuery { SourceType = sourceType }, ct);
        return Ok(result);
    }

    [HttpPost("mapping-rules")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateMappingRule(
        [FromBody] CreateMappingRuleCommand command, CancellationToken ct)
    {
        var ruleId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetMappingRules), new { }, ruleId);
    }

    [HttpPut("mapping-rules/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMappingRule(
        Guid id, [FromBody] UpdateMappingRuleCommand command, CancellationToken ct)
    {
        var cmd = command with { Id = id };
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    // ── Dead Letter Queue ─────────────────────────────────────────────────

    [HttpGet("dead-letter")]
    [ProducesResponseType(typeof(PagedResult<WebhookEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WebhookEventDto>>> GetDeadLetterEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sourceType = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDeadLetterEventsQuery
        {
            Page = page,
            PageSize = pageSize,
            SourceType = sourceType,
        }, ct);
        return Ok(result);
    }

    [HttpPost("dead-letter/{eventId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryWebhookEvent(Guid eventId, CancellationToken ct)
    {
        await _mediator.Send(new RetryWebhookEventCommand { EventId = eventId }, ct);
        return NoContent();
    }
}
