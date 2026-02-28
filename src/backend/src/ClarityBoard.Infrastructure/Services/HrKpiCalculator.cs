using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Calculates HR-domain KPIs. Base metrics (headcount, turnover rate, etc.)
/// are sourced from HR systems via webhooks. Derived KPIs (retention rate,
/// revenue per employee) are computed from base metrics and financial data
/// when available.
/// </summary>
public class HrKpiCalculator : IKpiCalculationService
{
    private readonly IAppDbContext _db;

    public HrKpiCalculator(IAppDbContext db)
    {
        _db = db;
    }

    public string CalculatorName => "HrKpiCalculator";

    public async Task<Dictionary<string, decimal?>> CalculateAsync(
        Guid entityId, DateOnly snapshotDate, CancellationToken ct = default)
    {
        var results = new Dictionary<string, decimal?>();

        // ---- Base KPIs (require HR system data) ----

        // hr.headcount: Count(Active Employees) - from HR system
        results["hr.headcount"] = await GetLatestSnapshotValue(
            entityId, "hr.headcount", snapshotDate, ct);

        // hr.turnover_rate: Departures / Avg Headcount * 100 - from HR system
        results["hr.turnover_rate"] = await GetLatestSnapshotValue(
            entityId, "hr.turnover_rate", snapshotDate, ct);

        // hr.cost_per_hire: Total Recruiting Cost / New Hires - from HR system
        results["hr.cost_per_hire"] = await GetLatestSnapshotValue(
            entityId, "hr.cost_per_hire", snapshotDate, ct);

        // hr.time_to_hire: Avg Days from Posting to Acceptance - from HR system
        results["hr.time_to_hire"] = await GetLatestSnapshotValue(
            entityId, "hr.time_to_hire", snapshotDate, ct);

        // hr.absence_rate: Absent Days / Working Days * 100 - from HR system
        results["hr.absence_rate"] = await GetLatestSnapshotValue(
            entityId, "hr.absence_rate", snapshotDate, ct);

        // hr.training_cost_per_employee: Training Budget / Headcount - from HR system
        results["hr.training_cost_per_employee"] = await GetLatestSnapshotValue(
            entityId, "hr.training_cost_per_employee", snapshotDate, ct);

        // ---- Derived KPIs ----

        // hr.retention_rate = 100 - turnover_rate
        results["hr.retention_rate"] = results["hr.turnover_rate"] is { } turnover
            ? Math.Round(100m - turnover, 2)
            : null;

        // hr.revenue_per_employee = Revenue / Headcount
        results["hr.revenue_per_employee"] = await CalculateRevenuePerEmployee(
            entityId, snapshotDate, results["hr.headcount"], ct);

        return results;
    }

    /// <summary>
    /// Calculates revenue per employee by dividing the financial revenue KPI
    /// by headcount. Attempts to source revenue from existing financial KPI
    /// snapshots. Returns null if either revenue or headcount is unavailable
    /// or headcount is zero.
    /// </summary>
    private async Task<decimal?> CalculateRevenuePerEmployee(
        Guid entityId, DateOnly snapshotDate, decimal? headcount, CancellationToken ct)
    {
        if (!headcount.HasValue || headcount.Value == 0m)
            return null;

        // Try to get revenue from the financial domain's snapshot
        var revenue = await GetLatestSnapshotValue(entityId, "financial.revenue", snapshotDate, ct);
        if (!revenue.HasValue)
            return null;

        return Math.Round(revenue.Value / headcount.Value, 2);
    }

    /// <summary>
    /// Retrieves the most recent KpiSnapshot value for a given KPI on or before
    /// the specified date. Returns null if no snapshot exists.
    /// </summary>
    private async Task<decimal?> GetLatestSnapshotValue(
        Guid entityId, string kpiId, DateOnly asOfDate, CancellationToken ct)
    {
        return await _db.KpiSnapshots
            .Where(s => s.EntityId == entityId
                        && s.KpiId == kpiId
                        && s.SnapshotDate <= asOfDate)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }
}
