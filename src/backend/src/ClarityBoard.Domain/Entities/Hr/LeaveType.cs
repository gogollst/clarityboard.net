namespace ClarityBoard.Domain.Entities.Hr;

public class LeaveType
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool RequiresApproval { get; private set; }
    public bool IsDeductedFromBalance { get; private set; }
    public int? MaxDaysPerYear { get; private set; }
    public string Color { get; private set; } = "#3b82f6";
    public bool IsActive { get; private set; }

    private LeaveType() { }

    public static LeaveType Create(Guid entityId, string name, string code,
        bool requiresApproval = true, bool isDeductedFromBalance = true,
        int? maxDaysPerYear = null, string color = "#3b82f6")
    => new()
    {
        Id                    = Guid.NewGuid(),
        EntityId              = entityId,
        Name                  = name,
        Code                  = code,
        RequiresApproval      = requiresApproval,
        IsDeductedFromBalance = isDeductedFromBalance,
        MaxDaysPerYear        = maxDaysPerYear,
        Color                 = color,
        IsActive              = true,
    };

    public void Deactivate() => IsActive = false;
}
