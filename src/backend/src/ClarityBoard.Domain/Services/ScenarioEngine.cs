using ClarityBoard.Domain.Entities.Scenario;

namespace ClarityBoard.Domain.Services;

/// <summary>
/// Default implementation of the scenario calculation engine.
/// Applies parameter adjustments to baseline KPI values and projects
/// results forward for the configured number of months.
/// </summary>
public sealed class ScenarioEngine : IScenarioEngine
{
    /// <summary>
    /// Parameter keys recognized by the engine and their associated KPI identifiers.
    /// </summary>
    private static readonly Dictionary<string, string[]> ParameterToKpiMap = new()
    {
        ["revenue_growth_rate"] = ["fin.revenue", "fin.revenue_growth"],
        ["cogs_percentage"] = ["fin.gross_margin", "fin.cogs"],
        ["operating_expense_rate"] = ["fin.ebitda_margin", "fin.operating_margin"],
        ["headcount_growth"] = ["hr.headcount", "hr.revenue_per_employee"],
        ["customer_churn_rate"] = ["sales.churn_rate", "sales.arr", "sales.mrr"],
        ["conversion_rate"] = ["sales.conversion_rate", "sales.pipeline_value"],
        ["marketing_spend"] = ["mkt.cac", "mkt.roas"],
        ["payment_terms_days"] = ["fin.dso", "fin.ccc"],
        ["inventory_turnover"] = ["fin.dio", "fin.ccc"],
        ["tax_rate"] = ["fin.effective_tax_rate", "fin.net_income"],
    };

    public Task<IReadOnlyList<ScenarioResult>> CalculateAsync(
        Scenario scenario, CancellationToken ct = default)
    {
        var results = new List<ScenarioResult>();

        foreach (var parameter in scenario.Parameters)
        {
            if (ct.IsCancellationRequested)
                break;

            // Determine which KPIs this parameter affects
            var affectedKpis = ParameterToKpiMap.TryGetValue(parameter.ParameterKey, out var kpis)
                ? kpis
                : [parameter.ParameterKey];

            // Calculate the adjustment factor
            var adjustmentFactor = parameter.BaseValue != 0
                ? parameter.AdjustedValue / parameter.BaseValue
                : 1m;

            foreach (var kpiId in affectedKpis)
            {
                for (var month = 1; month <= scenario.ProjectionMonths; month++)
                {
                    // Compound the adjustment over months for growth-type parameters
                    var compoundedFactor = IsGrowthParameter(parameter.ParameterKey)
                        ? (decimal)Math.Pow((double)adjustmentFactor, month / 12.0)
                        : adjustmentFactor;

                    var baselineValue = parameter.BaseValue;
                    var projectedValue = baselineValue * compoundedFactor;

                    var result = ScenarioResult.Create(
                        scenario.Id,
                        kpiId,
                        month,
                        projectedValue,
                        baselineValue);

                    results.Add(result);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<ScenarioResult>>(results);
    }

    private static bool IsGrowthParameter(string parameterKey) =>
        parameterKey.Contains("growth", StringComparison.OrdinalIgnoreCase)
        || parameterKey.Contains("rate", StringComparison.OrdinalIgnoreCase);
}
