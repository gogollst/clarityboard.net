namespace ClarityBoard.Domain.Entities.Budget;

public class BudgetLine
{
    public Guid Id { get; private set; }
    public Guid BudgetId { get; private set; }
    public Guid AccountId { get; private set; }
    public string? CostCenter { get; private set; }
    public short Month { get; private set; } // 1-12
    public decimal Amount { get; private set; }
    public decimal ActualAmount { get; private set; } // Updated from journal entries
    public decimal Variance { get; private set; } // Amount - ActualAmount
    public decimal VariancePct { get; private set; }
    public string? Notes { get; private set; }

    private BudgetLine() { }

    public static BudgetLine Create(
        Guid budgetId, Guid accountId, short month, decimal amount,
        string? costCenter = null, string? notes = null)
    {
        return new BudgetLine
        {
            Id = Guid.NewGuid(),
            BudgetId = budgetId,
            AccountId = accountId,
            Month = month,
            Amount = amount,
            CostCenter = costCenter,
            Notes = notes,
        };
    }

    public void UpdateActual(decimal actualAmount)
    {
        ActualAmount = actualAmount;
        Variance = Amount - actualAmount;
        VariancePct = Amount != 0 ? (Variance / Amount) * 100 : 0;
    }
}
