namespace ClarityBoard.Domain.Events;

public sealed record JournalEntryCreatedEvent(
    Guid JournalEntryId,
    Guid EntityId,
    long EntryNumber,
    DateOnly EntryDate,
    decimal TotalAmount) : DomainEvent;
