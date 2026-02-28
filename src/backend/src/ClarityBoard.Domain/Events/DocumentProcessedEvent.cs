namespace ClarityBoard.Domain.Events;

public sealed record DocumentProcessedEvent(
    Guid DocumentId,
    Guid EntityId,
    string Status,
    decimal? Confidence,
    string? VendorName,
    decimal? TotalAmount) : DomainEvent;
