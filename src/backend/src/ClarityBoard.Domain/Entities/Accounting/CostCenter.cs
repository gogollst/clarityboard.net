namespace ClarityBoard.Domain.Entities.Accounting;

public enum CostCenterType { Department, Project, Employee, Other }

public class CostCenter
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Code { get; private set; } = default!;
    public string ShortName { get; private set; } = default!;
    public string? Description { get; private set; }
    public CostCenterType Type { get; private set; }
    public Guid? ParentId { get; private set; }
    public Guid? HrEmployeeId { get; private set; }   // FK to hr.employees
    public Guid? HrDepartmentId { get; private set; } // FK to hr.departments
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private CostCenter() { }

    public static CostCenter Create(
        Guid entityId, string code, string shortName,
        CostCenterType type = CostCenterType.Other,
        Guid? hrEmployeeId = null, Guid? hrDepartmentId = null,
        string? description = null, Guid? parentId = null)
    {
        return new CostCenter
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Code = code,
            ShortName = shortName,
            Description = description,
            Type = type,
            ParentId = parentId,
            HrEmployeeId = hrEmployeeId,
            HrDepartmentId = hrDepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Deactivate() => IsActive = false;
}
