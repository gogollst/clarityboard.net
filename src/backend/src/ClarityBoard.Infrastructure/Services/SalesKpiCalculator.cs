using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Calculates sales-domain KPIs. Base metrics (MRR, ARPA, churn rate, etc.)
/// are sourced from external systems via webhooks and stored as KpiSnapshots.
/// Derived KPIs (ARR, MRR growth, CLV, LTV:CAC ratio) are computed from
/// the base metrics when available.
/// </summary>
public class SalesKpiCalculator : IKpiCalculationService
{
    private readonly IAppDbContext _db;

    public SalesKpiCalculator(IAppDbContext db)
    {
        _db = db;
    }

    public string CalculatorName => "SalesKpiCalculator";

    public async Task<Dictionary<string, decimal?>> CalculateAsync(
        Guid entityId, DateOnly snapshotDate, CancellationToken ct = default)
    {
        var results = new Dictionary<string, decimal?>();

        // ---- Base KPIs (require external data, return null if not yet ingested) ----
        // sales.mrr: needs subscription data from billing system
        results["sales.mrr"] = await GetLatestSnapshotValue(entityId, "sales.mrr", snapshotDate, ct);

        // sales.arpa: needs customer count from CRM/billing
        results["sales.arpa"] = await GetLatestSnapshotValue(entityId, "sales.arpa", snapshotDate, ct);

        // sales.churn_rate: needs customer churn data
        results["sales.churn_rate"] = await GetLatestSnapshotValue(entityId, "sales.churn_rate", snapshotDate, ct);

        // sales.cac: needs marketing/sales spend + new customer count
        results["sales.cac"] = await GetLatestSnapshotValue(entityId, "sales.cac", snapshotDate, ct);

        // sales.net_revenue_retention: needs MRR breakdown (expansion, contraction, churn)
        results["sales.net_revenue_retention"] = await GetLatestSnapshotValue(entityId, "sales.net_revenue_retention", snapshotDate, ct);

        // sales.pipeline_value: needs CRM opportunity data
        results["sales.pipeline_value"] = await GetLatestSnapshotValue(entityId, "sales.pipeline_value", snapshotDate, ct);

        // sales.win_rate: needs CRM deal outcome data
        results["sales.win_rate"] = await GetLatestSnapshotValue(entityId, "sales.win_rate", snapshotDate, ct);

        // sales.avg_deal_size: needs CRM deal data
        results["sales.avg_deal_size"] = await GetLatestSnapshotValue(entityId, "sales.avg_deal_size", snapshotDate, ct);

        // ---- Derived KPIs ----

        // sales.arr = MRR * 12
        results["sales.arr"] = results["sales.mrr"] is { } mrr
            ? Math.Round(mrr * 12m, 2)
            : null;

        // sales.mrr_growth = (Current MRR - Previous MRR) / Previous MRR * 100
        results["sales.mrr_growth"] = await CalculateMrrGrowth(entityId, snapshotDate, results["sales.mrr"], ct);

        // sales.clv = ARPA * Gross Margin Ratio * (1 / Churn Rate)
        results["sales.clv"] = CalculateClv(results);

        // sales.ltv_cac_ratio = CLV / CAC
        results["sales.ltv_cac_ratio"] = CalculateLtvCacRatio(results);

        return results;
    }

    /// <summary>
    /// Calculates MRR growth rate by comparing current MRR to the previous
    /// month's MRR snapshot. Returns null if either value is unavailable
    /// or previous MRR is zero.
    /// </summary>
    private async Task<decimal?> CalculateMrrGrowth(
        Guid entityId, DateOnly snapshotDate, decimal? currentMrr, CancellationToken ct)
    {
        if (!currentMrr.HasValue)
            return null;

        var previousDate = snapshotDate.AddMonths(-1);
        var previousMrr = await GetLatestSnapshotValue(entityId, "sales.mrr", previousDate, ct);

        if (!previousMrr.HasValue || previousMrr.Value == 0m)
            return null;

        return Math.Round((currentMrr.Value - previousMrr.Value) / Math.Abs(previousMrr.Value) * 100m, 2);
    }

    /// <summary>
    /// Calculates Customer Lifetime Value: ARPA * Gross Margin Ratio * (1 / Churn Rate).
    /// Gross margin ratio is sourced from the financial domain's gross_margin KPI.
    /// Returns null if any input is missing or churn rate is zero.
    /// </summary>
    private static decimal? CalculateClv(Dictionary<string, decimal?> results)
    {
        var arpa = results.GetValueOrDefault("sales.arpa");
        var churnRate = results.GetValueOrDefault("sales.churn_rate");

        if (!arpa.HasValue || !churnRate.HasValue || churnRate.Value == 0m)
            return null;

        // CLV = ARPA * (1 / monthly churn rate expressed as decimal)
        // Churn rate is stored as percentage, convert to decimal ratio
        var churnDecimal = churnRate.Value / 100m;
        if (churnDecimal == 0m)
            return null;

        return Math.Round(arpa.Value * (1m / churnDecimal), 2);
    }

    /// <summary>
    /// Calculates LTV:CAC ratio. Returns null if CLV or CAC is missing,
    /// or if CAC is zero.
    /// </summary>
    private static decimal? CalculateLtvCacRatio(Dictionary<string, decimal?> results)
    {
        var clv = results.GetValueOrDefault("sales.clv");
        var cac = results.GetValueOrDefault("sales.cac");

        if (!clv.HasValue || !cac.HasValue || cac.Value == 0m)
            return null;

        return Math.Round(clv.Value / cac.Value, 2);
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
