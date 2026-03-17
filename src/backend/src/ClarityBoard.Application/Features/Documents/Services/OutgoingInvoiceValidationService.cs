using ClarityBoard.Application.Common.Interfaces;

namespace ClarityBoard.Application.Features.Documents.Services;

public record ValidationResult(string RuleId, string Severity, string ReviewReasonKey, string? Detail = null);

public class OutgoingInvoiceValidationService
{
    /// <summary>
    /// Validates an outgoing invoice extraction result against rules V-01 to V-10.
    /// Returns a list of failed validations as review reasons.
    /// </summary>
    public IReadOnlyList<ValidationResult> Validate(DocumentExtractionResult extraction)
    {
        var results = new List<ValidationResult>();

        // V-01: Invoice number present and plausible format
        if (string.IsNullOrWhiteSpace(extraction.InvoiceNumber))
        {
            results.Add(new("V-01", "ERROR", "missing_invoice_number", "Invoice number is missing."));
        }

        // V-02: Invoice date not in the future
        if (extraction.InvoiceDate.HasValue && extraction.InvoiceDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            results.Add(new("V-02", "ERROR", "invalid_date",
                $"Invoice date {extraction.InvoiceDate.Value} is in the future."));
        }

        // V-03: Total amount > 0 (unless credit note)
        if (extraction.GrossAmount.HasValue && extraction.GrossAmount.Value <= 0)
        {
            results.Add(new("V-03", "ERROR", "invalid_amount",
                $"Total amount {extraction.GrossAmount.Value} is not positive."));
        }

        // V-04: Service period required for license line items
        var hasLicenseItems = extraction.LineItems.Any(li =>
            li.ProductCategory is "SAAS_LICENSE" or "ON_PREM_LICENSE" or "HOSTING" or "MAINTENANCE");

        if (hasLicenseItems && !extraction.ServicePeriodStart.HasValue)
        {
            // Check if at least the line items have service periods
            var licenseItemsWithoutPeriod = extraction.LineItems
                .Where(li => li.ProductCategory is "SAAS_LICENSE" or "ON_PREM_LICENSE" or "HOSTING" or "MAINTENANCE")
                .Where(li => !li.ServicePeriodStart.HasValue)
                .ToList();

            if (licenseItemsWithoutPeriod.Count > 0)
            {
                results.Add(new("V-04", "WARNING", "missing_service_period",
                    $"{licenseItemsWithoutPeriod.Count} license/service line item(s) without service period."));
            }
        }

        // V-05: Service period end > start
        if (extraction.ServicePeriodStart.HasValue && extraction.ServicePeriodEnd.HasValue
            && extraction.ServicePeriodEnd.Value <= extraction.ServicePeriodStart.Value)
        {
            results.Add(new("V-05", "ERROR", "invalid_service_period",
                $"Service period end ({extraction.ServicePeriodEnd.Value}) must be after start ({extraction.ServicePeriodStart.Value})."));
        }

        // V-06: Service period ≤ 36 months (plausibility)
        if (extraction.ServicePeriodStart.HasValue && extraction.ServicePeriodEnd.HasValue)
        {
            var months = ((extraction.ServicePeriodEnd.Value.Year - extraction.ServicePeriodStart.Value.Year) * 12)
                         + extraction.ServicePeriodEnd.Value.Month - extraction.ServicePeriodStart.Value.Month;
            if (months > 36)
            {
                results.Add(new("V-06", "WARNING", "unusual_service_period",
                    $"Service period of {months} months exceeds 36 months."));
            }
        }

        // V-07: Sum of line items = subtotal (if both available)
        if (extraction.LineItems.Count > 0 && extraction.NetAmount.HasValue)
        {
            var lineItemSum = extraction.LineItems
                .Where(li => li.TotalPrice.HasValue)
                .Sum(li => li.TotalPrice!.Value);

            if (lineItemSum > 0 && Math.Abs(lineItemSum - extraction.NetAmount.Value) > 0.05m)
            {
                results.Add(new("V-07", "ERROR", "amount_mismatch",
                    $"Line item sum ({lineItemSum:F2}) differs from subtotal ({extraction.NetAmount.Value:F2})."));
            }
        }

        // V-08: VAT correctly calculated (tolerance 0.02 EUR)
        if (extraction.NetAmount.HasValue && extraction.TaxAmount.HasValue && extraction.GrossAmount.HasValue)
        {
            var expectedGross = extraction.NetAmount.Value + extraction.TaxAmount.Value;
            if (Math.Abs(expectedGross - extraction.GrossAmount.Value) > 0.02m)
            {
                results.Add(new("V-08", "WARNING", "vat_mismatch",
                    $"Net ({extraction.NetAmount.Value:F2}) + Tax ({extraction.TaxAmount.Value:F2}) = {expectedGross:F2}, but gross is {extraction.GrossAmount.Value:F2}."));
            }
        }

        // V-09: Reverse charge + VAT rate must be 0%
        if (extraction.ReverseCharge && extraction.TaxAmount.HasValue && extraction.TaxAmount.Value > 0)
        {
            results.Add(new("V-09", "ERROR", "reverse_charge_vat_conflict",
                $"Reverse charge invoice has non-zero VAT amount ({extraction.TaxAmount.Value:F2})."));
        }

        // V-10: Customer needs VAT ID for EU cross-border
        if (!string.IsNullOrEmpty(extraction.RecipientCountry)
            && extraction.RecipientCountry != "DE"
            && IsEuCountry(extraction.RecipientCountry)
            && string.IsNullOrWhiteSpace(extraction.RecipientVatId))
        {
            results.Add(new("V-10", "WARNING", "missing_customer_vat",
                $"EU customer in {extraction.RecipientCountry} without VAT ID."));
        }

        return results;
    }

    private static bool IsEuCountry(string countryCode) =>
        countryCode is "AT" or "BE" or "BG" or "HR" or "CY" or "CZ" or "DK"
            or "EE" or "FI" or "FR" or "GR" or "HU" or "IE" or "IT" or "LV"
            or "LT" or "LU" or "MT" or "NL" or "PL" or "PT" or "RO" or "SK"
            or "SI" or "ES" or "SE";
}
