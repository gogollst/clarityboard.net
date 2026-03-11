namespace ClarityBoard.Domain.Entities.Accounting;

public enum PlanEntrySource { Manual, HrSync, Import, Rollover }

public class AccountingPlanEntry
{
    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid AccountId { get; private set; }
    public short PeriodYear { get; private set; }
    public short PeriodMonth { get; private set; }
    public long AmountCents { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? HrEmployeeId { get; private set; }
    public PlanEntrySource Source { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private AccountingPlanEntry() { }

    public static AccountingPlanEntry Create(
        Guid scenarioId, Guid entityId, Guid accountId,
        short year, short month, long amountCents,
        Guid createdBy, PlanEntrySource source = PlanEntrySource.Manual,
        Guid? costCenterId = null, Guid? hrEmployeeId = null, string? notes = null)
    {
        return new AccountingPlanEntry
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            EntityId = entityId,
            AccountId = accountId,
            PeriodYear = year,
            PeriodMonth = month,
            AmountCents = amountCents,
            CostCenterId = costCenterId,
            HrEmployeeId = hrEmployeeId,
            Source = source,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }
}
