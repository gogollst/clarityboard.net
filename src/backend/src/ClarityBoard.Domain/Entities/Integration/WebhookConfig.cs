namespace ClarityBoard.Domain.Entities.Integration;

public class WebhookConfig
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string SourceType { get; private set; } = default!; // stripe, hubspot, personio, custom
    public string Name { get; private set; } = default!;
    public string EndpointPath { get; private set; } = default!; // /api/webhooks/{source_type}/{id}
    public string? SecretKey { get; private set; } // Encrypted webhook signing secret
    public string? HeaderSignatureKey { get; private set; } // Header name containing signature
    public bool IsActive { get; private set; } = true;
    public string? EventFilter { get; private set; } // JSON: list of event types to accept
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastReceivedAt { get; private set; }

    private WebhookConfig() { }

    public static WebhookConfig Create(
        Guid entityId, string sourceType, string name, string endpointPath,
        string? secretKey = null, string? headerSignatureKey = null)
    {
        return new WebhookConfig
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            SourceType = sourceType,
            Name = name,
            EndpointPath = endpointPath,
            SecretKey = secretKey,
            HeaderSignatureKey = headerSignatureKey,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? secretKey, string? headerSignatureKey, string? eventFilter, bool isActive)
    {
        Name = name;
        SecretKey = secretKey;
        HeaderSignatureKey = headerSignatureKey;
        EventFilter = eventFilter;
        IsActive = isActive;
    }

    public void RecordReceived() => LastReceivedAt = DateTime.UtcNow;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
