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

        // Handle nested supplier/vendor objects: { supplier: { name, address: { street, city, ... }, tax_id } }
        var supplierObj = GetNestedObject(root, "supplier", "vendor");

        return new DocumentExtractionResult
        {
            VendorName = GetString(root, "vendor_name", "vendorName", "supplier_name")
                         ?? GetNestedString(supplierObj, "name"),
            VendorTaxId = GetString(root, "vendor_tax_id", "vendorTaxId", "supplier_tax_id", "ust_id_nr")
                          ?? GetNestedString(supplierObj, "tax_id", "taxId", "ust_id_nr", "vat_id"),
            VendorStreet = GetString(root, "vendor_street", "vendorStreet", "supplier_street")
                           ?? GetNestedAddressField(supplierObj, "street"),
            VendorCity = GetString(root, "vendor_city", "vendorCity", "supplier_city")
                         ?? GetNestedAddressField(supplierObj, "city"),
            VendorPostalCode = GetString(root, "vendor_postal_code", "vendorPostalCode", "supplier_postal_code")
                               ?? GetNestedAddressField(supplierObj, "postal_code", "postalCode", "zip"),
            VendorCountry = GetString(root, "vendor_country", "vendorCountry", "supplier_country")
                            ?? GetNestedAddressField(supplierObj, "country"),
            VendorIban = GetString(root, "vendor_iban", "vendorIban", "supplier_iban", "iban")
                         ?? GetNestedString(supplierObj, "iban"),
            VendorBic = GetString(root, "vendor_bic", "vendorBic", "supplier_bic", "bic")
                        ?? GetNestedString(supplierObj, "bic"),
            RecipientName = GetString(root, "recipient_name", "recipientName", "customer_name")
                            ?? GetNestedString(GetNestedObject(root, "recipient", "customer"), "name"),
            RecipientTaxId = GetString(root, "recipient_tax_id", "recipientTaxId", "customer_tax_id")
                             ?? GetNestedString(GetNestedObject(root, "recipient", "customer"), "tax_id", "taxId"),
            RecipientVatId = GetString(root, "recipient_vat_id", "recipientVatId", "customer_vat_id", "recipient_ust_id_nr")
                             ?? GetNestedString(GetNestedObject(root, "recipient", "customer"), "vat_id", "ust_id_nr"),
            RecipientStreet = GetString(root, "recipient_street", "recipientStreet")
                              ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "street"),
            RecipientCity = GetString(root, "recipient_city", "recipientCity")
                            ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "city"),
            RecipientPostalCode = GetString(root, "recipient_postal_code", "recipientPostalCode")
                                  ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "postal_code", "postalCode", "zip"),
            RecipientCountry = GetString(root, "recipient_country", "recipientCountry")
                               ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "country"),
            DocumentDirection = GetString(root, "document_direction", "documentDirection", "direction"),
            InvoiceNumber = GetString(root, "invoice_number", "invoiceNumber", "document_number"),
            InvoiceDate = ParseDateOnly(GetString(root, "invoice_date", "invoiceDate", "date")),
            TotalAmount = GetDecimal(root, "total_amount", "totalAmount", "gross_amount", "total_gross", "bruttobetrag"),
            GrossAmount = GetDecimal(root, "gross_amount", "grossAmount", "total_gross", "bruttobetrag", "total_amount", "totalAmount"),
            NetAmount = GetDecimal(root, "net_amount", "netAmount", "total_net", "nettobetrag"),
            TaxAmount = GetDecimal(root, "tax_amount", "taxAmount", "total_tax", "vat_amount", "steuerbetrag", "ust_betrag"),
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
        string chartOfAccounts,
        IReadOnlyList<AccountInfo> accounts,
        string? companyContext,
        CancellationToken ct)
    {
        var accountsSummary = string.Join("\n", accounts.Select(a => $"{a.AccountNumber} {a.Name} ({a.AccountType})"));

        var response = await promptAiService.ExecuteAsync(
            "document.booking_suggestion",
            new Dictionary<string, string>
            {
                ["entity_id"] = entityId.ToString(),
                ["chart_of_accounts"] = chartOfAccounts,
                ["accounts_json"] = accountsSummary,
                ["extraction_json"] = JsonSerializer.Serialize(extraction),
                ["company_context"] = companyContext ?? string.Empty,
                ["holding_name"] = string.Empty,
                ["entities_context"] = string.Empty,
                ["cost_centers"] = string.Empty,
            },
            ct);

        using var doc = JsonDocument.Parse(ExtractJsonObject(response.Content));
        var root = doc.RootElement;

        var rawVatCode = GetString(root, "vat_code", "vatCode", "tax_code");
        var rawTaxKey = GetString(root, "tax_key", "taxKey", "bu_schluessel");

        return new BookingSuggestionResult
        {
            // Existing fields (backward compatible)
            DebitAccountNumber = GetString(root, "debit_account_number", "debitAccountNumber", "debit_account"),
            CreditAccountNumber = GetString(root, "credit_account_number", "creditAccountNumber", "credit_account"),
            Amount = GetDecimal(root, "amount") ?? extraction.TotalAmount ?? 0m,
            VatCode = NormalizeVatCode(rawVatCode, rawTaxKey),
            Description = GetString(root, "description", "booking_text"),
            Confidence = GetDecimal(root, "confidence") ?? 0m,
            Reasoning = GetString(root, "reasoning", "rationale"),

            // New classification fields
            InvoiceType = GetString(root, "invoice_type", "invoiceType"),
            TaxKey = NormalizeTaxKey(rawTaxKey),
            AssignedEntity = GetString(root, "assigned_entity", "assignedEntity"),
            Notes = GetString(root, "notes"),
            VatTreatment = ParseVatTreatment(root),
            Flags = ParseFlags(root),
            ClassifiedLineItems = ParseClassifiedLineItems(root),
            BookingEntries = ParseBookingEntries(root),
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

        // Strip markdown code fences if present
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```", StringComparison.Ordinal))
                trimmed = trimmed[..^3].TrimEnd();
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            return trimmed[firstBrace..(lastBrace + 1)];

        throw new InvalidOperationException(
            $"AI response does not contain a JSON object. Response starts with: '{trimmed[..Math.Min(trimmed.Length, 100)]}'");
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

    /// <summary>
    /// Normalize tax key: strip "BU " prefix, return just the number (e.g. "BU 9" → "9").
    /// </summary>
    private static string? NormalizeTaxKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("BU ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..].Trim();
        return trimmed;
    }

    private static readonly HashSet<string> ValidVatCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "VSt19", "VSt7", "USt19", "USt7", "steuerfrei", "§13b",
    };

    private static readonly Dictionary<string, string> TaxKeyToVatCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["9"] = "VSt19", ["BU 9"] = "VSt19",
        ["5"] = "VSt7",  ["BU 5"] = "VSt7",  ["8"] = "VSt7", ["BU 8"] = "VSt7",
        ["3"] = "USt19", ["BU 3"] = "USt19",
        ["2"] = "USt7",  ["BU 2"] = "USt7",
        ["19"] = "§13b", ["BU 19"] = "§13b",
        ["10"] = "steuerfrei", ["BU 10"] = "steuerfrei",
        ["11"] = "steuerfrei", ["BU 11"] = "steuerfrei",
        ["0"] = "steuerfrei", ["BU 0"] = "steuerfrei",
    };

    /// <summary>
    /// Normalize VatCode: if the AI returned a BU key instead of a proper VatCode, map it.
    /// </summary>
    private static string? NormalizeVatCode(string? rawVatCode, string? rawTaxKey)
    {
        // If vat_code is already a valid value, use it
        if (!string.IsNullOrWhiteSpace(rawVatCode) && ValidVatCodes.Contains(rawVatCode.Trim()))
            return rawVatCode.Trim();

        // If vat_code looks like a BU key (e.g. "BU 9"), map it
        if (!string.IsNullOrWhiteSpace(rawVatCode) && TaxKeyToVatCode.TryGetValue(rawVatCode.Trim(), out var mapped))
            return mapped;

        // Fall back: derive from tax_key if vat_code is missing/invalid
        if (!string.IsNullOrWhiteSpace(rawTaxKey) && TaxKeyToVatCode.TryGetValue(rawTaxKey.Trim(), out var derived))
            return derived;

        return rawVatCode?.Trim();
    }

    private static JsonElement? GetNestedObject(JsonElement root, params string[] names)
    {
        if (TryGetProperty(root, out var obj, names) && obj.ValueKind == JsonValueKind.Object)
            return obj;
        return null;
    }

    private static string? GetNestedString(JsonElement? obj, params string[] names)
    {
        if (obj is null) return null;
        return GetString(obj.Value, names);
    }

    private static string? GetNestedAddressField(JsonElement? supplierObj, params string[] fieldNames)
    {
        if (supplierObj is null) return null;
        // Try direct field first: supplier.street
        var direct = GetString(supplierObj.Value, fieldNames);
        if (direct is not null) return direct;
        // Try nested address object: supplier.address.street
        if (TryGetProperty(supplierObj.Value, out var addressObj, "address") && addressObj.ValueKind == JsonValueKind.Object)
            return GetString(addressObj, fieldNames);
        return null;
    }

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

    private static bool GetBool(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names))
            return false;

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(property.GetString(), out var parsed) && parsed,
            _ => false,
        };
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names) || property.ValueKind != JsonValueKind.Array)
            return [];

        return property.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToList();
    }

    private static IReadOnlyList<ReviewReason> GetReviewReasonArray(JsonElement element, params string[] names)
    {
        if (!TryGetProperty(element, out var property, names) || property.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<ReviewReason>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                // Old format: plain string → treat as key with empty detail
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    results.Add(new ReviewReason { Key = value, Detail = "" });
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                // New format: { "key": "...", "detail": "..." }
                var key = GetString(item, "key");
                var detail = GetString(item, "detail") ?? "";
                if (!string.IsNullOrWhiteSpace(key))
                    results.Add(new ReviewReason { Key = key, Detail = detail });
            }
        }

        return results;
    }

    private static VatTreatmentResult? ParseVatTreatment(JsonElement root)
    {
        var obj = GetNestedObject(root, "vat_treatment", "vatTreatment");
        if (obj is null) return null;
        var vt = obj.Value;

        return new VatTreatmentResult
        {
            Type = GetString(vt, "type") ?? "standard_19",
            Explanation = GetString(vt, "explanation"),
            InputTaxAccount = GetString(vt, "input_tax_account", "inputTaxAccount"),
            InputTaxDeductible = GetBool(vt, "input_tax_deductible", "inputTaxDeductible"),
            OutputTaxAccount = GetString(vt, "output_tax_account", "outputTaxAccount"),
            LegalBasis = GetString(vt, "legal_basis", "legalBasis"),
        };
    }

    private static BookingFlagsResult? ParseFlags(JsonElement root)
    {
        var obj = GetNestedObject(root, "flags");
        if (obj is null) return null;
        var f = obj.Value;

        return new BookingFlagsResult
        {
            NeedsManualReview = GetBool(f, "needs_manual_review", "needsManualReview"),
            ReviewReasons = GetReviewReasonArray(f, "review_reasons", "reviewReasons"),
            IsRecurring = GetBool(f, "is_recurring", "isRecurring"),
            GwgRelevant = GetBool(f, "gwg_relevant", "gwgRelevant"),
            ActivationRequired = GetBool(f, "activation_required", "activationRequired"),
            ReverseCharge = GetBool(f, "reverse_charge", "reverseCharge"),
            IntraCommunity = GetBool(f, "intra_community", "intraCommunity"),
            EntertainmentExpense = GetBool(f, "entertainment_expense", "entertainmentExpense"),
        };
    }

    private static IReadOnlyList<ClassifiedLineItemResult> ParseClassifiedLineItems(JsonElement root)
    {
        if (!TryGetProperty(root, out var items, "classified_line_items", "classifiedLineItems") || items.ValueKind != JsonValueKind.Array)
            return [];

        return items.EnumerateArray().Select(item => new ClassifiedLineItemResult
        {
            Description = GetString(item, "description"),
            NetAmount = GetDecimal(item, "net_amount", "netAmount") ?? 0m,
            VatRate = GetDecimal(item, "vat_rate", "vatRate") ?? 0m,
            VatAmount = GetDecimal(item, "vat_amount", "vatAmount") ?? 0m,
            AccountNumber = GetString(item, "account_number", "accountNumber"),
            AccountName = GetString(item, "account_name", "accountName"),
            CostCenter = GetString(item, "cost_center", "costCenter"),
        }).ToList();
    }

    private static IReadOnlyList<BookingEntryResult> ParseBookingEntries(JsonElement root)
    {
        if (!TryGetProperty(root, out var entries, "booking_entries", "bookingEntries") || entries.ValueKind != JsonValueKind.Array)
            return [];

        return entries.EnumerateArray().Select(entry => new BookingEntryResult
        {
            DebitAccount = GetString(entry, "debit_account", "debitAccount"),
            DebitAccountName = GetString(entry, "debit_account_name", "debitAccountName"),
            CreditAccount = GetString(entry, "credit_account", "creditAccount"),
            CreditAccountName = GetString(entry, "credit_account_name", "creditAccountName"),
            Amount = GetDecimal(entry, "amount") ?? 0m,
            TaxKey = GetString(entry, "tax_key", "taxKey"),
            Description = GetString(entry, "booking_text", "bookingText", "description"),
        }).ToList();
    }
}