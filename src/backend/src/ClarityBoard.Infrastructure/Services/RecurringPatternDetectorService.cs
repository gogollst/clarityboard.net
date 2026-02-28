using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Document;
using ClarityBoard.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services;

/// <summary>
/// Scans the Document table for vendors with 3+ invoices and groups by
/// VendorName + similar amounts (within 10% tolerance).
/// Creates or updates RecurringPattern entities for matching vendors.
/// </summary>
public class RecurringPatternDetectorService : IRecurringPatternDetector
{
    private readonly IAppDbContext _db;
    private readonly ILogger<RecurringPatternDetectorService> _logger;

    private const int MinInvoicesForPattern = 3;
    private const decimal AmountTolerancePercent = 0.10m; // 10%

    public RecurringPatternDetectorService(IAppDbContext db, ILogger<RecurringPatternDetectorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectedPattern>> DetectPatternsAsync(
        Guid entityId, CancellationToken ct = default)
    {
        // 1. Load all documents with extracted data for this entity
        var documents = await _db.Documents
            .Where(d => d.EntityId == entityId
                        && d.VendorName != null
                        && d.TotalAmount != null
                        && (d.Status == "extracted" || d.Status == "booked"))
            .Select(d => new
            {
                d.Id,
                VendorName = d.VendorName!,
                TotalAmount = d.TotalAmount!.Value,
            })
            .ToListAsync(ct);

        // 2. Group by vendor name
        var vendorGroups = documents
            .GroupBy(d => d.VendorName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= MinInvoicesForPattern);

        var detectedPatterns = new List<DetectedPattern>();

        foreach (var group in vendorGroups)
        {
            var invoices = group.OrderBy(d => d.TotalAmount).ToList();

            // 3. Cluster invoices by similar amounts (within 10% tolerance of cluster median)
            var clusters = ClusterByAmount(invoices.Select(i => i.TotalAmount).ToList());

            foreach (var cluster in clusters.Where(c => c.Count >= MinInvoicesForPattern))
            {
                var avgAmount = cluster.Average();
                var confidence = Math.Min(0.95m, 0.60m + (cluster.Count * 0.05m));

                // 4. Look for existing booking suggestions to derive account info
                var docIds = invoices.Select(i => i.Id).ToList();
                var existingSuggestion = await _db.BookingSuggestions
                    .Where(bs => docIds.Contains(bs.DocumentId)
                                 && bs.Status == "accepted"
                                 && bs.EntityId == entityId)
                    .OrderByDescending(bs => bs.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                detectedPatterns.Add(new DetectedPattern
                {
                    VendorName = group.Key,
                    AverageAmount = Math.Round(avgAmount, 2),
                    InvoiceCount = cluster.Count,
                    SuggestedDebitAccountId = existingSuggestion?.DebitAccountId,
                    SuggestedCreditAccountId = existingSuggestion?.CreditAccountId,
                    SuggestedVatCode = existingSuggestion?.VatCode,
                    Confidence = confidence,
                });
            }
        }

        // 5. Upsert RecurringPattern entities
        await UpsertPatternsAsync(entityId, detectedPatterns, ct);

        _logger.LogInformation("Detected {Count} recurring patterns for entity {EntityId}",
            detectedPatterns.Count, entityId);

        return detectedPatterns;
    }

    /// <summary>
    /// Clusters amounts into groups where each value is within 10% of the group average.
    /// Uses a simple greedy approach on sorted amounts.
    /// </summary>
    private static List<List<decimal>> ClusterByAmount(List<decimal> sortedAmounts)
    {
        if (sortedAmounts.Count == 0) return [];

        var clusters = new List<List<decimal>>();
        var currentCluster = new List<decimal> { sortedAmounts[0] };

        for (var i = 1; i < sortedAmounts.Count; i++)
        {
            var clusterAvg = currentCluster.Average();

            // Check if within tolerance of cluster average
            if (clusterAvg == 0 || Math.Abs(sortedAmounts[i] - clusterAvg) / Math.Abs(clusterAvg) <= AmountTolerancePercent)
            {
                currentCluster.Add(sortedAmounts[i]);
            }
            else
            {
                clusters.Add(currentCluster);
                currentCluster = [sortedAmounts[i]];
            }
        }

        clusters.Add(currentCluster);
        return clusters;
    }

    private async Task UpsertPatternsAsync(
        Guid entityId, List<DetectedPattern> detected, CancellationToken ct)
    {
        var existing = await _db.RecurringPatterns
            .Where(rp => rp.EntityId == entityId && rp.IsActive)
            .ToListAsync(ct);

        foreach (var pattern in detected)
        {
            var match = existing.FirstOrDefault(rp =>
                string.Equals(rp.VendorName, pattern.VendorName, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                // Update existing -- just increment match count
                match.IncrementMatch();
            }
            else if (pattern.SuggestedDebitAccountId.HasValue && pattern.SuggestedCreditAccountId.HasValue)
            {
                // Create new pattern only if we have account suggestions
                var newPattern = RecurringPattern.Create(
                    entityId,
                    pattern.VendorName,
                    pattern.SuggestedDebitAccountId.Value,
                    pattern.SuggestedCreditAccountId.Value,
                    pattern.SuggestedVatCode,
                    costCenter: null,
                    pattern.Confidence);

                _db.RecurringPatterns.Add(newPattern);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
