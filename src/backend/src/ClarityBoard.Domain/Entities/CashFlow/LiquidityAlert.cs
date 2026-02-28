namespace ClarityBoard.Domain.Entities.CashFlow;

public class LiquidityAlert
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string AlertType { get; private set; } = default!; // cash_below_minimum, runway_low, runway_critical
    public decimal ThresholdValue { get; private set; }
    public decimal CurrentValue { get; private set; }
    public string Severity { get; private set; } = default!;
    public string Status { get; private set; } = "active";
    public DateTime TriggeredAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private LiquidityAlert() { }

    public static LiquidityAlert Create(
        Guid entityId, string alertType, decimal thresholdValue,
        decimal currentValue, string severity)
    {
        return new LiquidityAlert
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            AlertType = alertType,
            ThresholdValue = thresholdValue,
            CurrentValue = currentValue,
            Severity = severity,
            TriggeredAt = DateTime.UtcNow,
        };
    }

    public void Resolve()
    {
        Status = "resolved";
        ResolvedAt = DateTime.UtcNow;
    }
}
