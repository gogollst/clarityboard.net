using ClarityBoard.Domain.Entities.Scenario;

namespace ClarityBoard.Domain.Services;

/// <summary>
/// Engine that applies scenario parameter adjustments to baseline KPI values
/// and recalculates projected results for the configured projection horizon.
/// </summary>
public interface IScenarioEngine
{
    /// <summary>
    /// Calculates scenario results by applying parameter adjustments to baseline KPI values.
    /// Returns a list of <see cref="ScenarioResult"/> containing projected values per KPI per month.
    /// </summary>
    Task<IReadOnlyList<ScenarioResult>> CalculateAsync(
        Scenario scenario, CancellationToken ct = default);
}
