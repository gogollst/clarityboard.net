namespace ClarityBoard.Domain.Entities.AI;

/// <summary>
/// Immutable audit trail of every prompt change.
/// Analogous to the JournalEntry hash-chain for accounting immutability.
/// </summary>
public class AiPromptVersion
{
    public Guid Id { get; private set; }
    public Guid PromptId { get; private set; }
    public int Version { get; private set; }
    public string SystemPrompt { get; private set; } = default!;
    public string? UserPromptTemplate { get; private set; }
    public AiProvider PrimaryProvider { get; private set; }
    public AiProvider FallbackProvider { get; private set; }

    /// <summary>Required: describes what changed and why.</summary>
    public string ChangeSummary { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private AiPromptVersion() { }

    public static AiPromptVersion Create(
        Guid promptId,
        int version,
        string systemPrompt,
        string? userPromptTemplate,
        AiProvider primaryProvider,
        AiProvider fallbackProvider,
        string changeSummary,
        Guid createdByUserId)
    {
        return new AiPromptVersion
        {
            Id                 = Guid.NewGuid(),
            PromptId           = promptId,
            Version            = version,
            SystemPrompt       = systemPrompt,
            UserPromptTemplate = userPromptTemplate,
            PrimaryProvider    = primaryProvider,
            FallbackProvider   = fallbackProvider,
            ChangeSummary      = changeSummary,
            CreatedAt          = DateTime.UtcNow,
            CreatedByUserId    = createdByUserId,
        };
    }
}

