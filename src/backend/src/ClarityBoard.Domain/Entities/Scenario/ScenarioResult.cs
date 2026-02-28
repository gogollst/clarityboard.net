namespace ClarityBoard.Domain.Entities.Scenario;

public class ScenarioResult
{
    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public string KpiId { get; private set; } = default!;
    public int Month { get; private set; } // Projection month (1-based)
    public decimal ProjectedValue { get; private set; }
    public decimal BaselineValue { get; private set; }
    public decimal DeltaValue { get; private set; }
    public decimal DeltaPct { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private ScenarioResult() { }

    public static ScenarioResult Create(
        Guid scenarioId, string kpiId, int month,
        decimal projectedValue, decimal baselineValue)
    {
        var delta = projectedValue - baselineValue;
        return new ScenarioResult
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            KpiId = kpiId,
            Month = month,
            ProjectedValue = projectedValue,
            BaselineValue = baselineValue,
            DeltaValue = delta,
            DeltaPct = baselineValue != 0 ? (delta / baselineValue) * 100 : 0,
            CalculatedAt = DateTime.UtcNow,
        };
    }
}
