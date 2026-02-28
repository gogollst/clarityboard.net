namespace ClarityBoard.Domain.Entities.KPI;

public class KpiAlert
{
    public static readonly string[] ValidConditions =
        ["lt", "gt", "lte", "gte", "eq", "change_pct_gt"];

    public static readonly string[] ValidSeverities =
        ["critical", "warning", "info"];

    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string KpiId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Condition { get; private set; } = default!; // lt, gt, lte, gte, eq, change_pct_gt
    public decimal ThresholdValue { get; private set; }
    public string Severity { get; private set; } = default!; // critical, warning, info
    public string TargetRoles { get; private set; } = "[]"; // JSON array of role strings
    public string Channels { get; private set; } = "[]"; // JSON: ["dashboard", "email", "sms"]
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private KpiAlert() { }

    public static KpiAlert Create(
        Guid entityId,
        string kpiId,
        string name,
        string condition,
        decimal thresholdValue,
        string severity,
        string? targetRoles = null,
        string? channels = null)
    {
        return new KpiAlert
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            KpiId = kpiId,
            Name = name,
            Condition = condition,
            ThresholdValue = thresholdValue,
            Severity = severity,
            TargetRoles = targetRoles ?? "[]",
            Channels = channels ?? "[]",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        string condition,
        decimal thresholdValue,
        string severity,
        string? targetRoles = null,
        string? channels = null)
    {
        Name = name;
        Condition = condition;
        ThresholdValue = thresholdValue;
        Severity = severity;
        TargetRoles = targetRoles ?? TargetRoles;
        Channels = channels ?? Channels;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
