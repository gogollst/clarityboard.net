namespace ClarityBoard.Domain.Entities.KPI;

public class KpiAlertEvent
{
    public Guid Id { get; private set; }
    public Guid AlertId { get; private set; }
    public Guid EntityId { get; private set; }
    public string KpiId { get; private set; } = default!;
    public decimal CurrentValue { get; private set; }
    public decimal ThresholdValue { get; private set; }
    public string Severity { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string Status { get; private set; } = "active"; // active, acknowledged, resolved
    public Guid? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private KpiAlertEvent() { }

    public static KpiAlertEvent Create(
        Guid alertId,
        Guid entityId,
        string kpiId,
        decimal currentValue,
        decimal thresholdValue,
        string severity,
        string title,
        string message)
    {
        return new KpiAlertEvent
        {
            Id = Guid.NewGuid(),
            AlertId = alertId,
            EntityId = entityId,
            KpiId = kpiId,
            CurrentValue = currentValue,
            ThresholdValue = thresholdValue,
            Severity = severity,
            Title = title,
            Message = message,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Acknowledge(Guid userId)
    {
        Status = "acknowledged";
        AcknowledgedBy = userId;
        AcknowledgedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        Status = "resolved";
        ResolvedAt = DateTime.UtcNow;
    }
}
