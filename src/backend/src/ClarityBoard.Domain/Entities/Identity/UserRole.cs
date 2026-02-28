namespace ClarityBoard.Domain.Entities.Identity;

public class UserRole
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid EntityId { get; private set; } // Role is scoped to an entity
    public DateTime AssignedAt { get; private set; }
    public Guid AssignedBy { get; private set; }

    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId, Guid entityId, Guid assignedBy)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            EntityId = entityId,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow,
        };
    }
}
