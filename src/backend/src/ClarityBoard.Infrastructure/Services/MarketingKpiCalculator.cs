using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Calculates marketing-domain KPIs. All marketing metrics depend on
/// external tool data (Google Analytics, Mailchimp, HubSpot, etc.)
/// ingested via webhooks. When external data has been stored as
/// KpiSnapshots, derived values will be returned; otherwise null.
/// </summary>
public class MarketingKpiCalculator : IKpiCalculationService
{
    private readonly IAppDbContext _db;

    public MarketingKpiCalculator(IAppDbContext db)
    {
        _db = db;
    }

    public string CalculatorName => "MarketingKpiCalculator";

    public async Task<Dictionary<string, decimal?>> CalculateAsync(
        Guid entityId, DateOnly snapshotDate, CancellationToken ct = default)
    {
        var results = new Dictionary<string, decimal?>();

        // All marketing KPIs require external data from marketing tools
        // (Google Analytics, Mailchimp, HubSpot, ad platforms, etc.).
        // Values are ingested via webhooks and stored as KpiSnapshots.
        // We return the latest available snapshot value if one exists,
        // otherwise null to indicate missing data.

        // marketing.cpl: Marketing Spend / Leads Generated
        // Source: Ad platforms (Google Ads, LinkedIn Ads, etc.)
        results["marketing.cpl"] = await GetLatestSnapshotValue(
            entityId, "marketing.cpl", snapshotDate, ct);

        // marketing.marketing_roi: (Revenue from Marketing - Marketing Cost) / Marketing Cost * 100
        // Source: Attribution platform + ad spend data
        results["marketing.marketing_roi"] = await GetLatestSnapshotValue(
            entityId, "marketing.marketing_roi", snapshotDate, ct);

        // marketing.lead_conversion_rate: Customers / Leads * 100
        // Source: CRM + marketing automation
        results["marketing.lead_conversion_rate"] = await GetLatestSnapshotValue(
            entityId, "marketing.lead_conversion_rate", snapshotDate, ct);

        // marketing.website_conversion: Conversions / Visitors * 100
        // Source: Google Analytics or similar
        results["marketing.website_conversion"] = await GetLatestSnapshotValue(
            entityId, "marketing.website_conversion", snapshotDate, ct);

        // marketing.email_open_rate: Opens / Sent * 100
        // Source: Email marketing platform (Mailchimp, SendGrid, etc.)
        results["marketing.email_open_rate"] = await GetLatestSnapshotValue(
            entityId, "marketing.email_open_rate", snapshotDate, ct);

        // marketing.cpa: Marketing Spend / New Customers
        // Source: Ad platforms + CRM
        results["marketing.cpa"] = await GetLatestSnapshotValue(
            entityId, "marketing.cpa", snapshotDate, ct);

        return results;
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
