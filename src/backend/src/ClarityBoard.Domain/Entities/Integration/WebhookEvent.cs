namespace ClarityBoard.Domain.Entities.Integration;

public class WebhookEvent
{
    public Guid Id { get; private set; }
    public string SourceType { get; private set; } = default!;
    public string SourceId { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public string IdempotencyKey { get; private set; } = default!;
    public string Payload { get; private set; } = default!; // JSON
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string Status { get; private set; } = "pending"; // pending, processing, completed, failed, dead_letter
    public string? ErrorMessage { get; private set; }
    public short RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public Guid? EntityId { get; private set; }
    public Guid? MappingRuleId { get; private set; }
    public int? ProcessingDurationMs { get; private set; }

    private WebhookEvent() { }

    public static WebhookEvent Create(
        string sourceType, string sourceId, string eventType,
        string idempotencyKey, string payload)
    {
        return new WebhookEvent
        {
            Id = Guid.NewGuid(),
            SourceType = sourceType,
            SourceId = sourceId,
            EventType = eventType,
            IdempotencyKey = idempotencyKey,
            Payload = payload,
            ReceivedAt = DateTime.UtcNow,
        };
    }

    public void MarkProcessing() => Status = "processing";

    public void MarkCompleted()
    {
        Status = "completed";
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage, DateTime? nextRetryAt = null)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
        RetryCount++;
        NextRetryAt = nextRetryAt;
    }

    public void MarkDeadLetter()
    {
        Status = "dead_letter";
    }

    public void MarkNoMapping()
    {
        Status = "no_mapping";
        ProcessedAt = DateTime.UtcNow;
    }

    public void SetEntityId(Guid entityId) => EntityId = entityId;
    public void SetMappingRuleId(Guid mappingRuleId) => MappingRuleId = mappingRuleId;
    public void SetProcessingDuration(int durationMs) => ProcessingDurationMs = durationMs;

    public void ResetForRetry()
    {
        Status = "pending";
        ErrorMessage = null;
        NextRetryAt = null;
    }
}
