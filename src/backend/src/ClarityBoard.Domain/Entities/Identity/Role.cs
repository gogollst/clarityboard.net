namespace ClarityBoard.Domain.Entities.Identity;

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!; // owner, admin, accountant, controller, analyst, viewer, auditor
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; } // System roles cannot be deleted

    private readonly List<Permission> _permissions = new();
    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    public static Role Create(string name, string? description = null, bool isSystem = false)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystem = isSystem,
        };
    }

    public void AddPermission(Permission permission) => _permissions.Add(permission);
}
