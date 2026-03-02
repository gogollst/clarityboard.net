namespace ClarityBoard.Domain.Entities.AI;

/// <summary>
/// Audit log for every AI API call made via the prompt execution engine.
/// Used for cost tracking, debugging and quality monitoring.
/// </summary>
public class AiCallLog
{
    public Guid Id { get; private set; }
    public Guid PromptId { get; private set; }
    public AiProvider UsedProvider { get; private set; }

    /// <summary>true if the primary provider failed and the fallback was used.</summary>
    public bool UsedFallback { get; private set; }

    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public int DurationMs { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    public Guid? UserId { get; private set; }
    public Guid? EntityId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private AiCallLog() { }

    public static AiCallLog Create(
        Guid promptId,
        AiProvider usedProvider,
        bool usedFallback,
        int inputTokens,
        int outputTokens,
        int durationMs,
        bool isSuccess,
        string? errorMessage = null,
        Guid? userId = null,
        Guid? entityId = null)
    {
        return new AiCallLog
        {
            Id           = Guid.NewGuid(),
            PromptId     = promptId,
            UsedProvider = usedProvider,
            UsedFallback = usedFallback,
            InputTokens  = inputTokens,
            OutputTokens = outputTokens,
            DurationMs   = durationMs,
            IsSuccess    = isSuccess,
            ErrorMessage = errorMessage,
            UserId       = userId,
            EntityId     = entityId,
            CreatedAt    = DateTime.UtcNow,
        };
    }
}

