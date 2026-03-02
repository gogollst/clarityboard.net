using ClarityBoard.Domain.Entities.AI;

namespace ClarityBoard.Application.Features.AI.DTOs;

// ── Provider ─────────────────────────────────────────────────────────────────

public record AiProviderConfigDto
{
    public Guid Id { get; init; }
    public AiProvider Provider { get; init; }
    public string ProviderName { get; init; } = default!;

    /// <summary>Masked display, e.g. "****...abcd".</summary>
    public string KeyHint { get; init; } = default!;

    public bool IsActive { get; init; }
    public bool IsHealthy { get; init; }
    public DateTime? LastTestedAt { get; init; }
    public string? BaseUrl { get; init; }
    public string? ModelDefault { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ProviderTestResultDto
{
    public AiProvider Provider { get; init; }
    public bool IsHealthy { get; init; }
    public int DurationMs { get; init; }
    public string? ErrorMessage { get; init; }
}

// ── Prompts ──────────────────────────────────────────────────────────────────

public record AiPromptListDto
{
    public Guid Id { get; init; }
    public string PromptKey { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Module { get; init; } = default!;
    public AiProvider PrimaryProvider { get; init; }
    public AiProvider FallbackProvider { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystemPrompt { get; init; }
    public int Version { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record AiPromptDetailDto
{
    public Guid Id { get; init; }
    public string PromptKey { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Module { get; init; } = default!;
    public string FunctionDescription { get; init; } = default!;
    public string SystemPrompt { get; init; } = default!;
    public string? UserPromptTemplate { get; init; }
    public string? ExampleInput { get; init; }
    public string? ExampleOutput { get; init; }
    public AiProvider PrimaryProvider { get; init; }
    public string PrimaryModel { get; init; } = default!;
    public AiProvider FallbackProvider { get; init; }
    public string FallbackModel { get; init; } = default!;
    public decimal Temperature { get; init; }
    public int MaxTokens { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystemPrompt { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid? LastEditedByUserId { get; init; }
    public IReadOnlyList<AiPromptVersionDto> Versions { get; init; } = [];
}

public record AiPromptVersionDto
{
    public Guid Id { get; init; }
    public int Version { get; init; }
    public string SystemPrompt { get; init; } = default!;
    public string? UserPromptTemplate { get; init; }
    public AiProvider PrimaryProvider { get; init; }
    public AiProvider FallbackProvider { get; init; }
    public string ChangeSummary { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
}

// ── Call Logs ─────────────────────────────────────────────────────────────────

public record AiCallLogDto
{
    public Guid Id { get; init; }
    public Guid PromptId { get; init; }
    public string PromptKey { get; init; } = default!;
    public AiProvider UsedProvider { get; init; }
    public bool UsedFallback { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int DurationMs { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UserId { get; init; }
    public Guid? EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AiCallLogStatsDto
{
    public int TotalCalls { get; init; }
    public int SuccessfulCalls { get; init; }
    public double SuccessRate { get; init; }
    public int AvgDurationMs { get; init; }
    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public int FallbackCount { get; init; }
}

