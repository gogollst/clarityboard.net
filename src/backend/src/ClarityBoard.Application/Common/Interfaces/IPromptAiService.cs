using ClarityBoard.Domain.Entities.AI;

namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Central execution engine for all AI prompts.
/// Every AI call in the system must go through this service using a PromptKey.
/// Hardcoded prompt strings in application code are forbidden.
/// </summary>
public interface IPromptAiService
{
    /// <summary>
    /// Executes a prompt identified by <paramref name="promptKey"/>,
    /// substituting <paramref name="variables"/> into the UserPromptTemplate.
    /// Automatically falls back to the secondary provider on failure.
    /// </summary>
    Task<AiResponse> ExecuteAsync(
        string promptKey,
        Dictionary<string, string> variables,
        CancellationToken ct);

    /// <summary>
    /// Special admin operation: uses Anthropic exclusively to improve a prompt.
    /// Returns the enhanced prompt text as a preview (does NOT persist).
    /// </summary>
    Task<string> EnhancePromptAsync(
        string currentSystemPrompt,
        string? userTemplate,
        string description,
        string functionDescription,
        CancellationToken ct);

    /// <summary>
    /// Performs a lightweight connectivity test against the given provider.
    /// Returns true if the provider responded successfully.
    /// </summary>
    Task<bool> TestProviderAsync(AiProvider provider, CancellationToken ct);
}

// ── Result record ────────────────────────────────────────────────────────────

public record AiResponse
{
    public string Content { get; init; } = default!;
    public AiProvider UsedProvider { get; init; }
    public bool UsedFallback { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int DurationMs { get; init; }
}

