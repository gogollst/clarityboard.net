namespace ClarityBoard.Domain.Events;

public sealed record AlertTriggeredEvent(
    Guid AlertEventId,
    Guid EntityId,
    string KpiId,
    string Severity,
    string Title,
    decimal CurrentValue,
    decimal ThresholdValue) : DomainEvent;
