namespace ClarityBoard.Domain.Entities.AI;

/// <summary>
/// Stores encrypted API keys and configuration for each AI provider.
/// Exactly one active config per provider (enforced via unique index on Provider + IsActive).
/// </summary>
public class AiProviderConfig
{
    public Guid Id { get; private set; }
    public AiProvider Provider { get; private set; }

    /// <summary>AES-256-GCM encrypted API key.</summary>
    public string EncryptedApiKey { get; private set; } = default!;

    /// <summary>Last 4 characters of the raw key – shown in UI instead of the full key.</summary>
    public string KeyHint { get; private set; } = default!;

    public bool IsActive { get; private set; }
    public bool IsHealthy { get; private set; }
    public DateTime? LastTestedAt { get; private set; }

    /// <summary>Optional custom base URL (e.g. for self-hosted or Azure OpenAI endpoints).</summary>
    public string? BaseUrl { get; private set; }

    /// <summary>Default model identifier, e.g. "claude-sonnet-4-5".</summary>
    public string? ModelDefault { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    private AiProviderConfig() { }

    public static AiProviderConfig Create(
        AiProvider provider,
        string encryptedApiKey,
        string keyHint,
        Guid createdByUserId,
        string? baseUrl = null,
        string? modelDefault = null)
    {
        return new AiProviderConfig
        {
            Id               = Guid.NewGuid(),
            Provider         = provider,
            EncryptedApiKey  = encryptedApiKey,
            KeyHint          = keyHint,
            IsActive         = true,
            IsHealthy        = false,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow,
            CreatedByUserId  = createdByUserId,
            BaseUrl          = baseUrl,
            ModelDefault     = modelDefault,
        };
    }

    public void UpdateKey(string encryptedApiKey, string keyHint)
    {
        EncryptedApiKey = encryptedApiKey;
        KeyHint         = keyHint;
        IsHealthy       = false;
        UpdatedAt       = DateTime.UtcNow;
    }

    public void SetHealthStatus(bool isHealthy)
    {
        IsHealthy    = isHealthy;
        LastTestedAt = DateTime.UtcNow;
        UpdatedAt    = DateTime.UtcNow;
    }

    public void UpdateSettings(string? baseUrl, string? modelDefault)
    {
        BaseUrl      = baseUrl;
        ModelDefault = modelDefault;
        UpdatedAt    = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}

