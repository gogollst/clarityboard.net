namespace ClarityBoard.Domain.Events;

public sealed record KpiUpdatedEvent(
    Guid EntityId,
    string KpiId,
    decimal Value,
    decimal? PreviousValue,
    DateOnly SnapshotDate) : DomainEvent;
