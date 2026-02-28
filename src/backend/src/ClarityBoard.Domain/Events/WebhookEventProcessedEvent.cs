namespace ClarityBoard.Domain.Events;

public sealed record WebhookEventProcessedEvent(
    Guid WebhookEventId,
    string SourceType,
    string EventType,
    string Status,
    int? ProcessingDurationMs) : DomainEvent;
