namespace ClarityBoard.Domain.Entities.AI;

/// <summary>
/// Stores the available models per AI provider.
/// Admins manage this table to control which models appear in prompt config dropdowns.
/// </summary>
public class AiProviderModel
{
    public Guid Id { get; private set; }
    public AiProvider Provider { get; private set; }

    /// <summary>API model identifier, e.g. "claude-sonnet-4-6".</summary>
    public string ModelId { get; private set; } = default!;

    /// <summary>Human-friendly display name, e.g. "Claude Sonnet 4.6".</summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>Sort order within the provider's model list (lower = first).</summary>
    public int SortOrder { get; private set; }

    /// <summary>Optional description or usage hints shown in the UI.</summary>
    public string? Description { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AiProviderModel() { }

    public static AiProviderModel Create(
        AiProvider provider,
        string modelId,
        string displayName,
        int sortOrder = 0,
        string? description = null)
    {
        return new AiProviderModel
        {
            Id          = Guid.NewGuid(),
            Provider    = provider,
            ModelId     = modelId,
            DisplayName = displayName,
            SortOrder   = sortOrder,
            Description = description,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
        };
    }

    public void Update(string displayName, int sortOrder, string? description)
    {
        DisplayName = displayName;
        SortOrder   = sortOrder;
        Description = description;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
