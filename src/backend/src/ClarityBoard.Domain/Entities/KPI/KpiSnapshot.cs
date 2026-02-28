namespace ClarityBoard.Domain.Entities.KPI;

public class KpiSnapshot
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string KpiId { get; private set; } = default!;
    public DateOnly SnapshotDate { get; private set; }
    public decimal? Value { get; private set; }
    public decimal? PreviousValue { get; private set; }
    public decimal? ChangePct { get; private set; }
    public decimal? TargetValue { get; private set; }
    public string? Components { get; private set; } // JSON breakdown
    public bool IsProvisional { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private KpiSnapshot() { } // EF Core

    public static KpiSnapshot Create(
        Guid entityId,
        string kpiId,
        DateOnly snapshotDate,
        decimal? value,
        decimal? previousValue,
        decimal? targetValue,
        string? components = null)
    {
        decimal? changePct = previousValue.HasValue && previousValue != 0
            ? ((value - previousValue) / Math.Abs(previousValue.Value)) * 100
            : null;

        return new KpiSnapshot
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            KpiId = kpiId,
            SnapshotDate = snapshotDate,
            Value = value,
            PreviousValue = previousValue,
            ChangePct = changePct,
            TargetValue = targetValue,
            Components = components,
            IsProvisional = false,
            CalculatedAt = DateTime.UtcNow,
        };
    }
}
