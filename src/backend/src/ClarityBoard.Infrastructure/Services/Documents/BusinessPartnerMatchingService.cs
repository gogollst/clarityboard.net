using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services.Documents;

public class BusinessPartnerMatchingService : IBusinessPartnerMatchingService
{
    private readonly IAppDbContext _db;

    public BusinessPartnerMatchingService(IAppDbContext db) => _db = db;

    public async Task<BusinessPartnerMatchResult> MatchPartnerAsync(
        Guid entityId, string vendorName, string? taxId, string? iban, CancellationToken ct)
    {
        // Stage 0: Check RecurringPattern for a previously learned mapping
        var patternPartner = await _db.RecurringPatterns
            .Where(rp => rp.EntityId == entityId
                && rp.BusinessPartnerId.HasValue
                && rp.IsActive
                && rp.VendorName.ToLower() == vendorName.ToLower())
            .Select(rp => rp.BusinessPartnerId)
            .FirstOrDefaultAsync(ct);

        if (patternPartner.HasValue)
        {
            var partner = await _db.BusinessPartners
                .FirstOrDefaultAsync(bp => bp.Id == patternPartner.Value && bp.IsActive, ct);
            if (partner is not null)
                return new BusinessPartnerMatchResult(PartnerMatchType.Exact, partner, []);
        }

        // Stage 1: Exact match on TaxId
        if (!string.IsNullOrWhiteSpace(taxId))
        {
            var match = await _db.BusinessPartners
                .FirstOrDefaultAsync(bp => bp.EntityId == entityId && bp.IsActive
                    && bp.TaxId != null && bp.TaxId == taxId, ct);
            if (match is not null)
                return new BusinessPartnerMatchResult(PartnerMatchType.Exact, match, []);
        }

        // Stage 2: Exact match on IBAN
        if (!string.IsNullOrWhiteSpace(iban))
        {
            var normalizedIban = iban.Replace(" ", "").ToUpperInvariant();
            var match = await _db.BusinessPartners
                .FirstOrDefaultAsync(bp => bp.EntityId == entityId && bp.IsActive
                    && bp.Iban != null && bp.Iban.ToUpper() == normalizedIban, ct);
            if (match is not null)
                return new BusinessPartnerMatchResult(PartnerMatchType.Exact, match, []);
        }

        // Stage 3: Exact match on Name (case-insensitive)
        var nameMatch = await _db.BusinessPartners
            .FirstOrDefaultAsync(bp => bp.EntityId == entityId && bp.IsActive
                && bp.Name.ToLower() == vendorName.ToLower(), ct);
        if (nameMatch is not null)
            return new BusinessPartnerMatchResult(PartnerMatchType.Exact, nameMatch, []);

        // Stage 4: Fuzzy match on Name
        var allActivePartners = await _db.BusinessPartners
            .Where(bp => bp.EntityId == entityId && bp.IsActive)
            .ToListAsync(ct);

        var fuzzyMatches = allActivePartners
            .Where(bp => IsFuzzyMatch(vendorName, bp.Name))
            .ToList();

        if (fuzzyMatches.Count > 0)
            return new BusinessPartnerMatchResult(PartnerMatchType.Fuzzy, null, fuzzyMatches);

        // Stage 5: No match
        return new BusinessPartnerMatchResult(PartnerMatchType.None, null, []);
    }

    private static bool IsFuzzyMatch(string input, string candidate)
    {
        var inputLower = input.ToLowerInvariant();
        var candidateLower = candidate.ToLowerInvariant();

        // Substring match (either direction)
        if (candidateLower.Contains(inputLower) || inputLower.Contains(candidateLower))
            return true;

        // Levenshtein distance ≤ 3
        return LevenshteinDistance(inputLower, candidateLower) <= 3;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
