using System.Text.RegularExpressions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Entity;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

public partial class DocumentClassifierService : IDocumentClassifier
{
    private readonly ILogger<DocumentClassifierService> _logger;

    // Thresholds: ≥80 = auto-approve outgoing, 50-79 = review, <50 = incoming
    private const int AutoApproveThreshold = 80;
    private const int ReviewThreshold = 50;

    public DocumentClassifierService(ILogger<DocumentClassifierService> logger)
    {
        _logger = logger;
    }

    public Task<DocumentClassificationResult> ClassifyAsync(
        string ocrText, IReadOnlyList<LegalEntity> entities, CancellationToken ct)
    {
        var matchedRules = new List<string>();
        var score = 0;
        var textUpper = ocrText.ToUpperInvariant();

        // ── Rule 1: Aussteller-Match (40 Punkte) ──
        // If the issuer (sender) matches one of our legal entities → outgoing invoice
        foreach (var entity in entities)
        {
            var entityScore = ScoreIssuerMatch(textUpper, entity);
            if (entityScore > 0)
            {
                score += entityScore;
                matchedRules.Add($"ISSUER_MATCH:{entity.Name} (+{entityScore})");
                break; // Only count the best match
            }
        }

        // ── Rule 2: Produktname/SaaS-Indikatoren (20 Punkte) ──
        var productScore = ScoreProductIndicators(textUpper);
        if (productScore > 0)
        {
            score += productScore;
            matchedRules.Add($"PRODUCT_INDICATOR (+{productScore})");
        }

        // ── Rule 3: Layout-Indikatoren (15 Punkte) ──
        var layoutScore = ScoreLayoutIndicators(textUpper);
        if (layoutScore > 0)
        {
            score += layoutScore;
            matchedRules.Add($"LAYOUT_INDICATOR (+{layoutScore})");
        }

        // ── Rule 4: Reverse-Charge-Hinweis (10 Punkte) ──
        var rcScore = ScoreReverseCharge(textUpper);
        if (rcScore > 0)
        {
            score += rcScore;
            matchedRules.Add($"REVERSE_CHARGE (+{rcScore})");
        }

        // ── Rule 5: Kontaktdaten-Match (10 Punkte) ──
        var contactScore = ScoreContactMatch(textUpper, entities);
        if (contactScore > 0)
        {
            score += contactScore;
            matchedRules.Add($"CONTACT_MATCH (+{contactScore})");
        }

        // ── Rule 6: Negativ-Signal (-30 Punkte) ──
        var negativeScore = ScoreNegativeSignals(textUpper, entities);
        if (negativeScore < 0)
        {
            score += negativeScore;
            matchedRules.Add($"NEGATIVE_SIGNAL ({negativeScore})");
        }

        // Clamp score to 0-100 range for confidence calculation
        var clampedScore = Math.Clamp(score, 0, 100);
        var confidence = clampedScore / 100m;

        string direction;
        if (score >= AutoApproveThreshold)
            direction = "outgoing";
        else if (score >= ReviewThreshold)
            direction = "outgoing"; // outgoing but needs review
        else
            direction = "incoming";

        _logger.LogInformation(
            "Document classified as {Direction} with score {Score} (confidence {Confidence:P0}). Rules: {Rules}",
            direction, score, confidence, string.Join(", ", matchedRules));

        return Task.FromResult(new DocumentClassificationResult(direction, confidence, score, matchedRules));
    }

    /// <summary>
    /// Rule 1: Checks if the document issuer matches a known legal entity (40 points max).
    /// Matches IBAN, TaxId, VatId, or company name in the top portion of the document.
    /// </summary>
    private static int ScoreIssuerMatch(string textUpper, LegalEntity entity)
    {
        // Check IBAN match (strongest signal)
        if (!string.IsNullOrEmpty(entity.Iban))
        {
            var normalizedIban = entity.Iban.Replace(" ", "").ToUpperInvariant();
            if (textUpper.Replace(" ", "").Contains(normalizedIban))
                return 40;
        }

        // Check TaxId match
        if (!string.IsNullOrEmpty(entity.TaxId))
        {
            var normalizedTaxId = NormalizeTaxId(entity.TaxId);
            if (NormalizeTaxId(textUpper).Contains(normalizedTaxId))
                return 40;
        }

        // Check VatId match
        if (!string.IsNullOrEmpty(entity.VatId))
        {
            var normalizedVatId = entity.VatId.Replace(" ", "").ToUpperInvariant();
            if (textUpper.Replace(" ", "").Contains(normalizedVatId))
                return 40;
        }

        // Check company name (weaker signal)
        var normalizedName = NormalizeCompanyName(entity.Name);
        if (normalizedName.Length >= 4 && textUpper.Contains(normalizedName))
            return 30; // Slightly less confident with name-only match

        return 0;
    }

    /// <summary>
    /// Rule 2: SaaS/software product indicators (20 points max).
    /// </summary>
    private static int ScoreProductIndicators(string textUpper)
    {
        var indicators = new[]
        {
            "SAAS", "SOFTWARE LICENSE", "SOFTWARELIZENZ", "LIZENZGEBÜHR",
            "SUBSCRIPTION", "CLOUD HOSTING", "MAINTENANCE", "WARTUNG",
            "AQUA CLOUD", "AQUA TEST", "LICENSE FEE", "HOSTING FEE",
            "ANNUAL LICENSE", "JAHRESLIZENZ", "MONATSLIZENZ", "MONTHLY LICENSE"
        };

        var matchCount = indicators.Count(i => textUpper.Contains(i));
        return matchCount switch
        {
            >= 3 => 20,
            2 => 15,
            1 => 10,
            _ => 0
        };
    }

    /// <summary>
    /// Rule 3: Layout indicators suggesting outgoing invoice format (15 points max).
    /// </summary>
    private static int ScoreLayoutIndicators(string textUpper)
    {
        var score = 0;

        // "Rechnungsadresse" / "Invoice To" suggests we are the sender
        if (textUpper.Contains("RECHNUNGSADRESSE") || textUpper.Contains("INVOICE TO"))
            score += 8;

        // "Fälligkeitsdatum" / "Due Date" is typical for issued invoices
        if (textUpper.Contains("FÄLLIGKEITSDATUM") || textUpper.Contains("DUE DATE"))
            score += 4;

        // "Bestellnummer" / "Order Number" / "Order number"
        if (textUpper.Contains("BESTELLNUMMER") || textUpper.Contains("ORDER NUMBER"))
            score += 3;

        return Math.Min(score, 15);
    }

    /// <summary>
    /// Rule 4: Reverse Charge indicators (10 points).
    /// Reverse charge almost always means we are the seller billing an EU customer.
    /// </summary>
    private static int ScoreReverseCharge(string textUpper)
    {
        if (textUpper.Contains("REVERSE CHARGE") ||
            textUpper.Contains("STEUERSCHULDNERSCHAFT DES LEISTUNGSEMPFÄNGERS") ||
            textUpper.Contains("§13B") || textUpper.Contains("§ 13B"))
            return 10;

        return 0;
    }

    /// <summary>
    /// Rule 5: Contact data match — our phone/email/website appears (10 points max).
    /// </summary>
    private static int ScoreContactMatch(string textUpper, IReadOnlyList<LegalEntity> entities)
    {
        // Check if entity address components appear in the document header area
        foreach (var entity in entities)
        {
            var matches = 0;
            if (!string.IsNullOrEmpty(entity.Street) && textUpper.Contains(entity.Street.ToUpperInvariant()))
                matches++;
            if (!string.IsNullOrEmpty(entity.PostalCode) && textUpper.Contains(entity.PostalCode))
                matches++;
            if (!string.IsNullOrEmpty(entity.City) && textUpper.Contains(entity.City.ToUpperInvariant()))
                matches++;

            if (matches >= 2)
                return 10;
        }

        return 0;
    }

    /// <summary>
    /// Rule 6: Negative signals — if our entity appears as recipient, this is an incoming invoice (-30 points).
    /// </summary>
    private static int ScoreNegativeSignals(string textUpper, IReadOnlyList<LegalEntity> entities)
    {
        // Look for pattern: "An:" or "To:" followed by our company name (we are the recipient)
        foreach (var entity in entities)
        {
            var name = NormalizeCompanyName(entity.Name);
            if (name.Length < 4) continue;

            // Check if our name appears after recipient indicators
            var recipientPatterns = new[] { "AN:", "TO:", "EMPFÄNGER:", "RECIPIENT:", "BILL TO:" };
            foreach (var pattern in recipientPatterns)
            {
                var idx = textUpper.IndexOf(pattern, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    // Check if our name appears within 200 chars after the recipient indicator
                    var searchArea = textUpper.Substring(idx, Math.Min(200, textUpper.Length - idx));
                    if (searchArea.Contains(name))
                        return -30;
                }
            }
        }

        return 0;
    }

    private static string NormalizeTaxId(string taxId)
        => TaxIdNormalizerRegex().Replace(taxId.ToUpperInvariant(), "");

    private static string NormalizeCompanyName(string name)
        => CompanyLegalFormRegex()
            .Replace(name.ToUpperInvariant(), "")
            .Trim();

    [GeneratedRegex(@"[^A-Z0-9]")]
    private static partial Regex TaxIdNormalizerRegex();

    [GeneratedRegex(@"\b(GMBH|AG|UG|E\.V\.|OHG|KG|SE|GMBH\s*&\s*CO\.?\s*KG)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CompanyLegalFormRegex();
}
