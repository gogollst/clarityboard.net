namespace ClarityBoard.Domain.Entities.Budget;

public class Budget
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = default!;
    public short FiscalYear { get; private set; }
    public string BudgetType { get; private set; } = default!; // annual, quarterly, rolling
    public string Status { get; private set; } = "draft"; // draft, active, locked, archived
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public int? Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? Department { get; private set; }
    public string? Description { get; private set; }

    private readonly List<BudgetLine> _lines = new();
    public IReadOnlyCollection<BudgetLine> Lines => _lines.AsReadOnly();

    private Budget() { }

    public static Budget Create(
        Guid entityId, string name, short fiscalYear, string budgetType, Guid createdBy,
        string? department = null, string? description = null)
    {
        return new Budget
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            Name = name,
            FiscalYear = fiscalYear,
            BudgetType = budgetType,
            CreatedBy = createdBy,
            Department = department,
            Description = description,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void AddLine(BudgetLine line) => _lines.Add(line);

    public void Activate()
    {
        Status = "active";
        TotalAmount = _lines.Sum(l => l.Amount);
    }

    public void Lock() => Status = "locked";

    public void Approve(Guid userId)
    {
        ApprovedBy = userId;
        ApprovedAt = DateTime.UtcNow;
    }
}
