namespace ClarityBoard.Domain.Entities.Budget;

public class BudgetRevision
{
    public Guid Id { get; private set; }
    public Guid BudgetId { get; private set; }
    public int RevisionNumber { get; private set; }
    public string Reason { get; private set; } = default!;
    public string Changes { get; private set; } = default!; // JSON: list of changed lines with old/new values
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BudgetRevision() { }

    public static BudgetRevision Create(
        Guid budgetId, int revisionNumber, string reason, string changes, Guid createdBy)
    {
        return new BudgetRevision
        {
            Id = Guid.NewGuid(),
            BudgetId = budgetId,
            RevisionNumber = revisionNumber,
            Reason = reason,
            Changes = changes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
