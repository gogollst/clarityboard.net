using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.AI.Commands;
using ClarityBoard.Application.Features.AI.DTOs;
using ClarityBoard.Application.Features.AI.Queries;
using ClarityBoard.Domain.Entities.AI;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing AI provider API keys and prompts.
/// All routes require the Admin or SuperAdmin role.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/[controller]")]
public class AiManagementController : ControllerBase
{
    private readonly ISender _mediator;

    public AiManagementController(ISender mediator) => _mediator = mediator;

    // ── Provider Management ───────────────────────────────────────────────

    /// <summary>Returns all configured AI providers (API keys masked).</summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyList<AiProviderConfigDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AiProviderConfigDto>>> GetProviders(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAiProvidersQuery(), ct);
        return Ok(result);
    }

    /// <summary>Creates or updates the API key for a provider. Runs an automatic connectivity test.</summary>
    [HttpPut("providers/{provider}")]
    [ProducesResponseType(typeof(AiProviderConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiProviderConfigDto>> UpsertProvider(
        AiProvider provider,
        [FromBody] UpsertProviderRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpsertAiProviderCommand
        {
            Provider     = provider,
            ApiKey       = request.ApiKey,
            BaseUrl      = request.BaseUrl,
            ModelDefault = request.ModelDefault,
        }, ct);
        return Ok(result);
    }

    /// <summary>Manually triggers a connectivity test for the given provider.</summary>
    [HttpPost("providers/{provider}/test")]
    [ProducesResponseType(typeof(ProviderTestResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProviderTestResultDto>> TestProvider(
        AiProvider provider, CancellationToken ct)
    {
        var result = await _mediator.Send(new TestAiProviderCommand { Provider = provider }, ct);
        return Ok(result);
    }

    // ── Prompt Management ─────────────────────────────────────────────────

    /// <summary>Returns a list of all AI prompts, optionally filtered by module.</summary>
    [HttpGet("prompts")]
    [ProducesResponseType(typeof(IReadOnlyList<AiPromptListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AiPromptListDto>>> GetPrompts(
        [FromQuery] string? module, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAiPromptsQuery { Module = module }, ct);
        return Ok(result);
    }

    /// <summary>Returns full details of a prompt including its version history.</summary>
    [HttpGet("prompts/{promptKey}")]
    [ProducesResponseType(typeof(AiPromptDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiPromptDetailDto>> GetPromptDetail(
        string promptKey, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAiPromptDetailQuery { PromptKey = promptKey }, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Updates a prompt. Creates a new version entry in the audit trail.</summary>
    [HttpPut("prompts/{promptKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrompt(
        string promptKey,
        [FromBody] UpdateAiPromptCommand command,
        CancellationToken ct)
    {
        var cmd = command with { PromptKey = promptKey };
        await _mediator.Send(cmd, ct);
        return NoContent();
    }

    /// <summary>
    /// Runs the "Enhance Prompt" feature via Anthropic Claude.
    /// Returns a preview of the improved prompt – does NOT persist.
    /// </summary>
    [HttpPost("prompts/{promptKey}/enhance")]
    [ProducesResponseType(typeof(EnhancePromptResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnhancePromptResponseDto>> EnhancePrompt(
        string promptKey,
        [FromBody] EnhancePromptRequestDto request,
        CancellationToken ct)
    {
        var enhanced = await _mediator.Send(new EnhancePromptCommand
        {
            CurrentSystemPrompt = request.CurrentSystemPrompt,
            UserPromptTemplate  = request.UserPromptTemplate,
            Description         = request.Description,
            FunctionDescription = request.FunctionDescription,
        }, ct);

        return Ok(new EnhancePromptResponseDto { EnhancedSystemPrompt = enhanced });
    }

    /// <summary>Returns the full version history for a prompt.</summary>
    [HttpGet("prompts/{promptKey}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<AiPromptVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AiPromptVersionDto>>> GetPromptVersions(
        string promptKey, CancellationToken ct)
    {
        var detail = await _mediator.Send(new GetAiPromptDetailQuery { PromptKey = promptKey }, ct);
        if (detail is null) return NotFound();
        return Ok(detail.Versions);
    }

    /// <summary>Restores a previous version of a prompt. Creates an auto-snapshot of the current state.</summary>
    [HttpPost("prompts/{promptKey}/versions/{version:int}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestorePromptVersion(
        string promptKey, int version, CancellationToken ct)
    {
        await _mediator.Send(new RestorePromptVersionCommand { PromptKey = promptKey, Version = version }, ct);
        return NoContent();
    }

    // ── Call Logs ─────────────────────────────────────────────────────────

    /// <summary>Returns paginated AI call logs with optional filters.</summary>
    [HttpGet("call-logs")]
    [ProducesResponseType(typeof(PagedResult<AiCallLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AiCallLogDto>>> GetCallLogs(
        [FromQuery] string? promptKey,
        [FromQuery] AiProvider? provider,
        [FromQuery] bool? isSuccess,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAiCallLogsQuery
        {
            PromptKey = promptKey,
            Provider  = provider,
            IsSuccess = isSuccess,
            From      = from,
            To        = to,
            Page      = page,
            PageSize  = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>Returns aggregate statistics for AI call logs.</summary>
    [HttpGet("call-logs/stats")]
    [ProducesResponseType(typeof(AiCallLogStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiCallLogStatsDto>> GetCallLogStats(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAiCallLogStatsQuery { From = from, To = to }, ct);
        return Ok(result);
    }
}

// ── Request/Response models (controller-specific) ─────────────────────────────

public record UpsertProviderRequest
{
    public required string ApiKey { get; init; }
    public string? BaseUrl { get; init; }
    public string? ModelDefault { get; init; }
}

public record EnhancePromptRequestDto
{
    public required string CurrentSystemPrompt { get; init; }
    public string? UserPromptTemplate { get; init; }
    public required string Description { get; init; }
    public required string FunctionDescription { get; init; }
}

public record EnhancePromptResponseDto
{
    public string EnhancedSystemPrompt { get; init; } = default!;
}

