namespace ClarityBoard.Domain.Entities.Entity;

public class IntercompanyRule
{
    public Guid Id { get; private set; }
    public Guid ParentEntityId { get; private set; }
    public string RuleType { get; private set; } = default!; // revenue_elimination, receivable_payable, dividend
    public string SourceAccountPattern { get; private set; } = default!;
    public string TargetAccountPattern { get; private set; } = default!;
    public string EliminationMethod { get; private set; } = default!; // full, proportional
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private IntercompanyRule() { }
}
