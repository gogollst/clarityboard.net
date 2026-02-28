namespace ClarityBoard.Domain.Entities.Scenario;

public class ScenarioParameter
{
    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public string ParameterKey { get; private set; } = default!; // e.g. revenue_growth_rate, cogs_percentage
    public decimal BaseValue { get; private set; }
    public decimal AdjustedValue { get; private set; }
    public string? Unit { get; private set; } // percentage, currency, ratio
    public string? Description { get; private set; }

    private ScenarioParameter() { }

    public static ScenarioParameter Create(
        Guid scenarioId, string parameterKey, decimal baseValue, decimal adjustedValue,
        string? unit = null, string? description = null)
    {
        return new ScenarioParameter
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            ParameterKey = parameterKey,
            BaseValue = baseValue,
            AdjustedValue = adjustedValue,
            Unit = unit,
            Description = description,
        };
    }
}
