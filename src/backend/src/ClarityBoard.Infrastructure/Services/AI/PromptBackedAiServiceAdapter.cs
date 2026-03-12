using System.Globalization;
using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;

namespace ClarityBoard.Infrastructure.Services.AI;

/// <summary>
/// Bridges the legacy IAiService abstraction to the centralized prompt execution engine.
/// </summary>
public sealed class PromptBackedAiServiceAdapter(IPromptAiService promptAiService) : IAiService
{
    public async Task<DocumentExtractionResult> ExtractDocumentFieldsAsync(
        string documentText,
        string? mimeType,
        CancellationToken ct)
    {
        var response = await ExecuteWithPromptAliasesAsync(
            ["document_extraction", "document.ocr_extraction"],
            new Dictionary<string, string>
            {
                ["document_text"] = documentText,
                ["mime_type"] = mimeType ?? "application/octet-stream",
            },
            ct);

        using var doc = JsonDocument.Parse(ExtractJsonObject(response.Content));
        var root = doc.RootElement;

        return new DocumentExtractionResult
        {
            VendorName = GetString(root, "vendor_name", "vendorName", "supplier_name"),
            VendorTaxId = GetString(root, "vendor_tax_id", "vendorTaxId", "supplier_tax_id", "ust_id_nr"),
            VendorStreet = GetString(root, "vendor_street", "vendorStreet", "supplier_street"),
            VendorCity = GetString(root, "vendor_city", "vendorCity", "supplier_city"),
            VendorPostalCode = GetString(root, "vendor_postal_code", "vendorPostalCode", "supplier_postal_code"),
            VendorCountry = GetString(root, "vendor_country", "vendorCountry", "supplier_country"),
            VendorIban = GetString(root, "vendor_iban", "vendorIban", "supplier_iban", "iban"),
            VendorBic = GetString(root, "vendor_bic", "vendorBic", "supplier_bic", "bic"),
            InvoiceNumber = GetString(root, "invoice_number", "invoiceNumber", "document_number"),
            InvoiceDate = ParseDateOnly(GetString(root, "invoice_date", "invoiceDate", "date")),
            TotalAmount = GetDecimal(root, "total_amount", "totalAmount", "gross_amount"),
            Currency = GetString(root, "currency"),
            TaxRate = GetDecimal(root, "tax_rate", "taxRate", "vat_rate"),
            LineItems = GetLineItems(root),
            RawFields = GetRawFields(root),
            Confidence = GetDecimal(root, "confidence") ?? 0m,
        };
    }

    public async Task<BookingSuggestionResult> SuggestBookingAsync(
        DocumentExtractionResult extraction,
        Guid entityId,
        CancellationToken ct)
    {
        var response = await promptAiService.ExecuteAsync(
            "document.booking_suggestion",
            new Dictionary<string, string>
            {
                ["entity_id"] = entityId.ToString(),
                ["extraction_json"] = JsonSerializer.Serialize(extraction),
            },
            ct);

        using var doc = JsonDocument.Parse(ExtractJsonObject(response.Content));
        var root = doc.RootElement;

        return new BookingSuggestionResult
        {
            DebitAccountNumber = GetString(root, "debit_account_number", "debitAccountNumber", "debit_account"),
            CreditAccountNumber = GetString(root, "credit_account_number", "creditAccountNumber", "credit_account"),
            Amount = GetDecimal(root, "amount") ?? extraction.TotalAmount ?? 0m,
            VatCode = GetString(root, "vat_code", "vatCode", "tax_code"),
            Description = GetString(root, "description", "booking_text"),
            Confidence = GetDecimal(root, "confidence") ?? 0m,
            Reasoning = GetString(root, "reasoning", "rationale"),
        };
    }

    public async Task<string> AnalyzeKpiAsync(
        string kpiId,
        decimal value,
        decimal? previousValue,
        string? context,
        CancellationToken ct)
    {
        var response = await promptAiService.ExecuteAsync(
            "kpi.anomaly_detection",
            new Dictionary<string, string>
            {
                ["kpi_id"] = kpiId,
                ["current_value"] = value.ToString(CultureInfo.InvariantCulture),
                ["previous_value"] = previousValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ["context"] = context ?? string.Empty,
            },
            ct);

        return response.Content;
    }

    private async Task<AiResponse> ExecuteWithPromptAliasesAsync(
        IReadOnlyList<string> promptKeys,
        Dictionary<string, string> variables,
        CancellationToken ct)
    {
        Exception? lastError = null;

        foreach (var promptKey in promptKeys)
        {
            try
            {
                return await promptAiService.ExecuteAsync(promptKey, variables, ct);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("No usable AI prompt was found.");
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.Contains('{'))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                return trimmed[firstBrace..(lastBrace + 1)];
        }

        return trimmed;
    }

    private static IReadOnlyDictionary<string, string> GetRawFields(JsonElement root)
        => root.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());

    private static IReadOnlyList<LineItemResult> GetLineItems(JsonElement root)
    {
        if (!TryGetProperty(root, out var lineItems, "line_items", "lineItems") || lineItems.ValueKind != JsonValueKind.Array)
            return [];

        return lineItems.EnumerateArray().Select(item => new LineItemResult
        {
            Description = GetString(item, "description", "name"),
            Quantity = GetDecimal(item, "quantity", "qty"),
            UnitPrice = GetDecimal(item, "unit_price", "unitPrice"),
            TotalPrice = GetDecimal(item, "total_price", "totalPrice", "amount"),
            TaxCode = GetString(item, "tax_code", "taxCode", "vat_code"),
        }).ToList();
    }

    private static DateOnly? ParseDateOnly(string? value)
        => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    private static decimal? GetDecimal(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static string? GetString(JsonElement element, params string[] names)
        => TryGetProperty(element, out var property, names) ? property.ToString() : null;

    private static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }
}