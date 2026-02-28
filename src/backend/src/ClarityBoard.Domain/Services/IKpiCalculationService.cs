namespace ClarityBoard.Domain.Services;

/// <summary>
/// Interface for KPI calculation engines. Each calculator implements
/// domain-specific KPI formulas (financial, sales, marketing, etc.).
/// </summary>
public interface IKpiCalculationService
{
    /// <summary>
    /// Gets the calculator class name that this service handles
    /// (must match KpiDefinition.CalculationClass).
    /// </summary>
    string CalculatorName { get; }

    /// <summary>
    /// Calculates all KPIs handled by this calculator for a given entity
    /// and snapshot date. Returns a dictionary of KPI ID to computed value.
    /// A null value indicates the KPI could not be computed (e.g., missing data,
    /// division by zero).
    /// </summary>
    Task<Dictionary<string, decimal?>> CalculateAsync(
        Guid entityId,
        DateOnly snapshotDate,
        CancellationToken ct = default);
}
