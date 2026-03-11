namespace ClarityBoard.Domain.Entities.Hr;

public class Department
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentDepartmentId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Department() { }

    public static Department Create(Guid entityId, string name, string code,
        Guid? parentDepartmentId = null, Guid? managerId = null, string? description = null)
    => new()
    {
        Id                 = Guid.NewGuid(),
        EntityId           = entityId,
        Name               = name,
        Code               = code,
        Description        = description,
        ParentDepartmentId = parentDepartmentId,
        ManagerId          = managerId,
        IsActive           = true,
        CreatedAt          = DateTime.UtcNow,
    };

    public void Update(string name, string code, Guid? parentDepartmentId, Guid? managerId, string? description = null)
    {
        Name               = name;
        Code               = code;
        Description        = description;
        ParentDepartmentId = parentDepartmentId;
        ManagerId          = managerId;
    }

    public void Deactivate() => IsActive = false;
}
