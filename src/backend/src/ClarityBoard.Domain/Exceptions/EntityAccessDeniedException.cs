namespace ClarityBoard.Domain.Exceptions;

public class EntityAccessDeniedException : DomainException
{
    public Guid EntityId { get; }
    public Guid UserId { get; }

    public EntityAccessDeniedException(Guid entityId, Guid userId)
        : base($"User '{userId}' does not have access to entity '{entityId}'.", "ENTITY_ACCESS_DENIED")
    {
        EntityId = entityId;
        UserId = userId;
    }
}
