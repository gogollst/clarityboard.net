using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.AI;

/// <summary>
/// Anthropic Claude integration using raw HttpClient with tool_use pattern for structured JSON output.
/// Includes PII masking for German tax numbers and IBAN patterns before sending to external API.
/// </summary>
public partial class ClaudeAiProvider : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeAiProvider> _logger;
    private readonly string _model;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public ClaudeAiProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ClaudeAiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = configuration["AI:Anthropic:Model"] ?? "claude-sonnet-4-20250514";

        // API key is configured via HttpClient factory (DI)
        var apiKey = configuration["AI:Anthropic:ApiKey"]
                     ?? throw new InvalidOperationException("AI:Anthropic:ApiKey is not configured.");

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
    }

    // ── IAiService ────────────────────────────────────────────────────────

    public async Task<DocumentExtractionResult> ExtractDocumentFieldsAsync(
        string documentText, string? mimeType, CancellationToken ct)
    {
        var maskedText = MaskPii(documentText);

        var tool = new ClaudeTool
        {
            Name = "extract_document_fields",
            Description = "Extract structured fields from an invoice or receipt document.",
            InputSchema = BuildExtractionSchema(),
        };

        var systemPrompt =
            "You are a German accounting document processor. Extract structured fields from invoices and receipts. " +
            "All amounts should be in their original currency. Dates should be in ISO format (YYYY-MM-DD). " +
            "The document follows German HGB accounting standards. Always use the extract_document_fields tool to return results.";

        var userMessage = $"Extract all fields from this document (content type: {mimeType ?? "unknown"}):\n\n{maskedText}";

        var response = await SendMessageWithToolAsync(systemPrompt, userMessage, tool, ct);

        return ParseExtractionResult(response);
    }

    public async Task<BookingSuggestionResult> SuggestBookingAsync(
        DocumentExtractionResult extraction, Guid entityId, CancellationToken ct)
    {
        var tool = new ClaudeTool
        {
            Name = "suggest_booking",
            Description = "Suggest a double-entry booking for a document based on German SKR03/SKR04 chart of accounts.",
            InputSchema = BuildBookingSchema(),
        };

        var systemPrompt =
            "You are a German accounting expert familiar with SKR03 and SKR04 charts of accounts. " +
            "Given extracted document fields, suggest the appropriate debit and credit accounts, " +
            "VAT code (VSt19, VSt7, USt19, USt7, etc.), and amount. Always use the suggest_booking tool to return results.";

        var extractionJson = JsonSerializer.Serialize(extraction, JsonOptions);
        var userMessage = $"Suggest a booking entry for this extracted document data:\n\n{extractionJson}";

        var response = await SendMessageWithToolAsync(systemPrompt, userMessage, tool, ct);

        return ParseBookingResult(response);
    }

    public async Task<string> AnalyzeKpiAsync(
        string kpiId, decimal value, decimal? previousValue, string? context, CancellationToken ct)
    {
        var systemPrompt =
            "You are a business intelligence analyst for a German enterprise. " +
            "Provide concise, actionable analysis of KPI values. " +
            "Reference German accounting standards (HGB) and tax law where relevant. " +
            "Respond in English. Keep your analysis under 200 words.";

        var userMessage = $"Analyze this KPI:\n" +
                          $"- KPI: {kpiId}\n" +
                          $"- Current Value: {value}\n" +
                          (previousValue.HasValue ? $"- Previous Value: {previousValue.Value}\n" : "") +
                          (context is not null ? $"- Context: {context}\n" : "");

        var request = BuildMessageRequest(systemPrompt, userMessage, tools: null);
        var response = await SendRequestAsync(request, ct);

        // Extract text from response content
        var textBlock = response.Content?.FirstOrDefault(c => c.Type == "text");
        return textBlock?.Text ?? "Unable to generate analysis.";
    }

    // ── PII Masking ──────────────────────────────────────────────────────

    internal static string MaskPii(string text)
    {
        // Mask German tax numbers (Steuernummer): various formats like 123/456/78901 or 12 345 67890
        text = SteuernummerRegex().Replace(text, "***STEUERNR***");

        // Mask IBAN: DE followed by 2 check digits and 18 digits
        text = IbanRegex().Replace(text, "***IBAN***");

        return text;
    }

    [GeneratedRegex(@"\b\d{2,3}[/\s]\d{3,4}[/\s]\d{4,5}\b")]
    private static partial Regex SteuernummerRegex();

    [GeneratedRegex(@"\b[A-Z]{2}\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{0,2}\b", RegexOptions.IgnoreCase)]
    private static partial Regex IbanRegex();

    // ── Claude API Communication ─────────────────────────────────────────

    private async Task<string> SendMessageWithToolAsync(
        string systemPrompt, string userMessage, ClaudeTool tool, CancellationToken ct)
    {
        var request = BuildMessageRequest(systemPrompt, userMessage, [tool]);
        var response = await SendRequestAsync(request, ct);

        // Find the tool_use block in the response
        var toolUse = response.Content?.FirstOrDefault(c => c.Type == "tool_use");
        if (toolUse?.Input is null)
        {
            _logger.LogWarning("Claude response did not contain expected tool_use block for tool {Tool}", tool.Name);
            throw new InvalidOperationException($"AI response did not contain expected structured output for {tool.Name}.");
        }

        // Input is a JsonElement from deserialization
        return toolUse.Input.Value.GetRawText();
    }

    private ClaudeMessageRequest BuildMessageRequest(
        string systemPrompt, string userMessage, IReadOnlyList<ClaudeTool>? tools)
    {
        var request = new ClaudeMessageRequest
        {
            Model = _model,
            MaxTokens = 4096,
            System = systemPrompt,
            Messages =
            [
                new ClaudeMessage
                {
                    Role = "user",
                    Content = userMessage,
                },
            ],
        };

        if (tools is { Count: > 0 })
        {
            request.Tools = tools;
            request.ToolChoice = new ClaudeToolChoice { Type = "any" };
        }

        return request;
    }

    private async Task<ClaudeMessageResponse> SendRequestAsync(
        ClaudeMessageRequest request, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending request to Claude API, model={Model}", _model);

        using var response = await _httpClient.PostAsync(ApiUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Claude API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Claude API request failed with status {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<ClaudeMessageResponse>(JsonOptions, ct)
                     ?? throw new InvalidOperationException("Failed to deserialize Claude API response.");

        _logger.LogDebug("Claude API response: stop_reason={StopReason}, usage={InputTokens}+{OutputTokens}",
            result.StopReason, result.Usage?.InputTokens, result.Usage?.OutputTokens);

        return result;
    }

    // ── Schema Builders ──────────────────────────────────────────────────

    private static JsonElement BuildExtractionSchema()
    {
        var schema = """
        {
          "type": "object",
          "properties": {
            "vendor_name": { "type": "string", "description": "Name of the vendor or supplier" },
            "invoice_number": { "type": "string", "description": "Invoice or receipt number" },
            "invoice_date": { "type": "string", "description": "Invoice date in ISO format YYYY-MM-DD" },
            "total_amount": { "type": "number", "description": "Total amount including tax" },
            "currency": { "type": "string", "description": "Currency code (EUR, USD, etc.)" },
            "tax_rate": { "type": "number", "description": "Primary tax rate as percentage (e.g. 19 for 19%)" },
            "confidence": { "type": "number", "description": "Confidence score 0.0-1.0" },
            "line_items": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "description": { "type": "string" },
                  "quantity": { "type": "number" },
                  "unit_price": { "type": "number" },
                  "total_price": { "type": "number" },
                  "tax_code": { "type": "string" }
                }
              }
            },
            "raw_fields": {
              "type": "object",
              "description": "Any additional fields found in the document as key-value pairs",
              "additionalProperties": { "type": "string" }
            }
          },
          "required": ["vendor_name", "total_amount", "confidence"]
        }
        """;

        return JsonDocument.Parse(schema).RootElement.Clone();
    }

    private static JsonElement BuildBookingSchema()
    {
        var schema = """
        {
          "type": "object",
          "properties": {
            "debit_account_number": { "type": "string", "description": "SKR03/SKR04 account number to debit" },
            "credit_account_number": { "type": "string", "description": "SKR03/SKR04 account number to credit" },
            "amount": { "type": "number", "description": "Booking amount" },
            "vat_code": { "type": "string", "description": "VAT code: VSt19, VSt7, USt19, USt7, or null for VAT-exempt" },
            "description": { "type": "string", "description": "Booking description for the journal entry" },
            "confidence": { "type": "number", "description": "Confidence score 0.0-1.0" },
            "reasoning": { "type": "string", "description": "Explanation of why these accounts were chosen" }
          },
          "required": ["debit_account_number", "credit_account_number", "amount", "confidence"]
        }
        """;

        return JsonDocument.Parse(schema).RootElement.Clone();
    }

    // ── Response Parsing ─────────────────────────────────────────────────

    private static DocumentExtractionResult ParseExtractionResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var lineItems = new List<LineItemResult>();
        if (root.TryGetProperty("line_items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                lineItems.Add(new LineItemResult
                {
                    Description = GetStringOrNull(item, "description"),
                    Quantity = GetDecimalOrNull(item, "quantity"),
                    UnitPrice = GetDecimalOrNull(item, "unit_price"),
                    TotalPrice = GetDecimalOrNull(item, "total_price"),
                    TaxCode = GetStringOrNull(item, "tax_code"),
                });
            }
        }

        var rawFields = new Dictionary<string, string>();
        if (root.TryGetProperty("raw_fields", out var fields) && fields.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in fields.EnumerateObject())
            {
                rawFields[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }

        DateOnly? invoiceDate = null;
        var dateStr = GetStringOrNull(root, "invoice_date");
        if (dateStr is not null && DateOnly.TryParse(dateStr, out var parsed))
            invoiceDate = parsed;

        return new DocumentExtractionResult
        {
            VendorName = GetStringOrNull(root, "vendor_name"),
            InvoiceNumber = GetStringOrNull(root, "invoice_number"),
            InvoiceDate = invoiceDate,
            TotalAmount = GetDecimalOrNull(root, "total_amount"),
            Currency = GetStringOrNull(root, "currency"),
            TaxRate = GetDecimalOrNull(root, "tax_rate"),
            Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDecimal() : 0.5m,
            LineItems = lineItems,
            RawFields = rawFields,
        };
    }

    private static BookingSuggestionResult ParseBookingResult(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new BookingSuggestionResult
        {
            DebitAccountNumber = GetStringOrNull(root, "debit_account_number"),
            CreditAccountNumber = GetStringOrNull(root, "credit_account_number"),
            Amount = root.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0,
            VatCode = GetStringOrNull(root, "vat_code"),
            Description = GetStringOrNull(root, "description"),
            Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDecimal() : 0.5m,
            Reasoning = GetStringOrNull(root, "reasoning"),
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string? GetStringOrNull(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static decimal? GetDecimalOrNull(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetDecimal() : null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    // ── Claude API DTOs (internal) ───────────────────────────────────────

    private sealed class ClaudeMessageRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("system")]
        public string? System { get; set; }

        [JsonPropertyName("messages")]
        public IReadOnlyList<ClaudeMessage> Messages { get; set; } = [];

        [JsonPropertyName("tools")]
        public IReadOnlyList<ClaudeTool>? Tools { get; set; }

        [JsonPropertyName("tool_choice")]
        public ClaudeToolChoice? ToolChoice { get; set; }
    }

    private sealed class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = default!;

        [JsonPropertyName("content")]
        public string Content { get; set; } = default!;
    }

    private sealed class ClaudeTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        public string Description { get; set; } = default!;

        [JsonPropertyName("input_schema")]
        public JsonElement InputSchema { get; set; }
    }

    private sealed class ClaudeToolChoice
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "any";
    }

    private sealed class ClaudeMessageResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("content")]
        public List<ClaudeContentBlock>? Content { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage? Usage { get; set; }
    }

    private sealed class ClaudeContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("input")]
        public JsonElement? Input { get; set; }
    }

    private sealed class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}
