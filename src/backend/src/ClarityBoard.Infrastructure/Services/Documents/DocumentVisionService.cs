using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

/// <summary>
/// Multimodal Vision/OCR service. Sends page images to Gemini (primary) or OpenAI (fallback)
/// and returns a normalized OCR result. Does NOT do structured field extraction — that remains
/// on the existing <see cref="IPromptAiService"/> / <see cref="IAiService"/> pipeline.
/// </summary>
public sealed class DocumentVisionService : IDocumentVisionService
{
    private const string PromptKey = "document.vision_ocr";
    private const int RequestTimeoutSeconds = 120;

    // Provider URL constants (duplicated from PromptAiService — Phase 2 will extract IAiProviderRuntimeResolver)
    private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    private const string GeminiRelativePath = "v1beta/openai/chat/completions";
    private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";
    private const string OpenAiRelativePath = "v1/chat/completions";
    private const string ZaiUrl = "https://api.z.ai/api/paas/v4/chat/completions";
    private const string ZaiRelativePath = "api/paas/v4/chat/completions";

    private static readonly TimeSpan PromptCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _sp;
    private readonly ICacheService _cache;
    private readonly IEncryptionService _encryption;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DocumentVisionService> _logger;

    public DocumentVisionService(
        IServiceProvider sp,
        ICacheService cache,
        IEncryptionService encryption,
        IHttpClientFactory httpClientFactory,
        ILogger<DocumentVisionService> logger)
    {
        _sp = sp;
        _cache = cache;
        _encryption = encryption;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DocumentOcrResult> ExtractTextAsync(
        DocumentVisionRequest request,
        CancellationToken ct = default)
    {
        if (request.PageImages.Count == 0)
        {
            return new DocumentOcrResult
            {
                FullText = string.Empty,
                Confidence = 0m,
                Warnings = ["no_page_images_provided"],
                Source = "vision",
            };
        }

        var prompt = await LoadPromptAsync(ct)
            ?? throw new InvalidOperationException($"AI prompt '{PromptKey}' not found or inactive.");

        var sw = Stopwatch.StartNew();
        var usedFallback = false;
        string usedProviderName;
        VisionCallResult? result = null;

        // Try primary provider (Gemini)
        try
        {
            result = await CallVisionProviderAsync(
                prompt.PrimaryProvider, prompt.PrimaryModel,
                prompt.SystemPrompt, request.PageImages,
                prompt.Temperature, prompt.MaxTokens, ct);
            usedProviderName = prompt.PrimaryProvider.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Primary vision provider {Provider} failed for document {DocumentId}. Trying fallback.",
                prompt.PrimaryProvider, request.DocumentId);

            // Try fallback provider (OpenAI)
            try
            {
                result = await CallVisionProviderAsync(
                    prompt.FallbackProvider, prompt.FallbackModel,
                    prompt.SystemPrompt, request.PageImages,
                    prompt.Temperature, prompt.MaxTokens, ct);
                usedProviderName = prompt.FallbackProvider.ToString();
                usedFallback = true;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx,
                    "Fallback vision provider {Provider} also failed for document {DocumentId}.",
                    prompt.FallbackProvider, request.DocumentId);
                sw.Stop();
                await WriteLogAsync(prompt.Id, prompt.FallbackProvider, true, 0, 0,
                    (int)sw.ElapsedMilliseconds, false, fallbackEx.Message, request.EntityId, ct);
                throw;
            }
        }

        sw.Stop();
        await WriteLogAsync(prompt.Id, usedFallback ? prompt.FallbackProvider : prompt.PrimaryProvider,
            usedFallback, result.InputTokens, result.OutputTokens, (int)sw.ElapsedMilliseconds, true, null, request.EntityId, ct);

        // Parse the JSON OCR result
        var ocrResult = ParseOcrResponse(result.Content, usedProviderName, usedFallback, (int)sw.ElapsedMilliseconds);

        _logger.LogInformation(
            "Vision OCR completed for document {DocumentId}: {TextLength} chars, confidence {Confidence:F2}, " +
            "{PageCount} pages, provider {Provider}, fallback {Fallback}, {Duration}ms",
            request.DocumentId, ocrResult.FullText.Length, ocrResult.Confidence,
            ocrResult.Pages.Count, usedProviderName, usedFallback, sw.ElapsedMilliseconds);

        return ocrResult;
    }

    // ── Provider Calls ──────────────────────────────────────────────────────

    private sealed record VisionCallResult(string Content, int InputTokens, int OutputTokens);

    private async Task<VisionCallResult> CallVisionProviderAsync(
        AiProvider provider, string model,
        string systemPrompt, IReadOnlyList<VisionPageInput> pages,
        decimal temperature, int maxTokens,
        CancellationToken ct)
    {
        var settings = await GetRequiredProviderSettingsAsync(provider, ct);
        var modelToUse = string.IsNullOrWhiteSpace(model)
            ? settings.ModelDefault ?? GetDefaultModel(provider)
            : model;

        var url = provider switch
        {
            AiProvider.Gemini => ResolveEndpoint(settings.BaseUrl, GeminiUrl, GeminiRelativePath),
            AiProvider.OpenAI => ResolveEndpoint(settings.BaseUrl, OpenAiUrl, OpenAiRelativePath),
            AiProvider.ZAI    => ResolveEndpoint(settings.BaseUrl, ZaiUrl, ZaiRelativePath),
            _ => throw new NotSupportedException($"Vision provider {provider} is not supported. Use Gemini, OpenAI, or ZAI."),
        };

        return await CallOpenAiCompatibleVisionAsync(
            settings.ApiKey, url, modelToUse, systemPrompt, pages,
            temperature, maxTokens, ct);
    }

    private async Task<VisionCallResult> CallOpenAiCompatibleVisionAsync(
        string apiKey, string url, string model,
        string systemPrompt, IReadOnlyList<VisionPageInput> pages,
        decimal temperature, int maxTokens,
        CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient("ai_vision");
        client.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Build multimodal user content: text instruction + page images
        var userContent = new List<object>
        {
            new { type = "text", text = BuildUserPrompt(pages) },
        };

        foreach (var page in pages)
        {
            var base64 = Convert.ToBase64String(page.ImageBytes);
            var dataUri = $"data:{page.MimeType};base64,{base64}";

            userContent.Add(new
            {
                type = "image_url",
                image_url = new { url = dataUri, detail = "high" },
            });
        }

        var requestBody = new
        {
            model,
            max_tokens = maxTokens,
            temperature = (double)temperature,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent },
            },
        };

        var body = JsonSerializer.Serialize(requestBody, s_snakeCaseOptions);
        using var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, httpContent, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Vision API {response.StatusCode}: {err}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        var text = ExtractContent(root);
        var inputTok = GetInt32OrDefault(root, "usage", "prompt_tokens");
        var outputTok = GetInt32OrDefault(root, "usage", "completion_tokens");

        return new VisionCallResult(text, inputTok, outputTok);
    }

    private static string BuildUserPrompt(IReadOnlyList<VisionPageInput> pages)
    {
        var pageCount = pages.Count;
        var pageLabels = string.Join(", ", pages.Select(page => $"page {page.PageNumber} ({page.MimeType})"));

        return pageCount == 1
            ? $"Extract all visible text from this accounting document image ({pageLabels}). Return only valid JSON."
            : $"Extract all visible text from these {pageCount} accounting document pages ({pageLabels}). Return only valid JSON with page-level text.";
    }

    // ── Response Parsing ────────────────────────────────────────────────────

    private DocumentOcrResult ParseOcrResponse(
        string rawContent, string usedProvider, bool usedFallback, int durationMs)
    {
        var warnings = new List<string>();
        if (usedFallback)
            warnings.Add("vision_provider_fallback_used");

        // Try to extract JSON from the response (may be wrapped in markdown code blocks)
        var jsonContent = ExtractJson(rawContent);
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            // If no valid JSON, treat the entire response as plain text
            _logger.LogWarning("Vision response was not valid JSON, treating as plain text");
            warnings.Add("vision_response_not_json");

            return new DocumentOcrResult
            {
                FullText = rawContent.Trim(),
                Confidence = 0.5m,
                Warnings = warnings,
                UsedProvider = usedProvider,
                UsedFallback = usedFallback,
                Source = "vision",
                DurationMs = durationMs,
                Pages = [new DocumentOcrPageResult(1, rawContent.Trim(), 0.5m, ["raw_text_fallback"])],
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var fullText = GetStringProp(root, "full_text") ?? string.Empty;
            var confidence = GetDecimalProp(root, "confidence") ?? 0.8m;

            if (root.TryGetProperty("warnings", out var warningsEl) && warningsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var w in warningsEl.EnumerateArray())
                {
                    var ws = w.GetString();
                    if (!string.IsNullOrWhiteSpace(ws))
                        warnings.Add(ws);
                }
            }

            var pages = new List<DocumentOcrPageResult>();
            if (root.TryGetProperty("pages", out var pagesEl) && pagesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var pageEl in pagesEl.EnumerateArray())
                {
                    var pageNum = pageEl.TryGetProperty("page_number", out var pn) ? pn.GetInt32() : pages.Count + 1;
                    var pageText = GetStringProp(pageEl, "text") ?? string.Empty;
                    var pageConf = GetDecimalProp(pageEl, "confidence") ?? confidence;
                    var pageWarnings = new List<string>();

                    if (pageEl.TryGetProperty("warnings", out var pw) && pw.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var w in pw.EnumerateArray())
                        {
                            var ws = w.GetString();
                            if (!string.IsNullOrWhiteSpace(ws))
                                pageWarnings.Add(ws);
                        }
                    }

                    pages.Add(new DocumentOcrPageResult(pageNum, pageText, pageConf, pageWarnings));
                }
            }

            // If full_text is empty but pages have text, concatenate
            if (string.IsNullOrWhiteSpace(fullText) && pages.Count > 0)
            {
                fullText = string.Join("\n\n", pages.Select(p => p.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
            }

            if (string.IsNullOrWhiteSpace(fullText))
                warnings.Add("vision_ocr_empty");

            return new DocumentOcrResult
            {
                FullText = fullText,
                Pages = pages,
                Confidence = confidence,
                Warnings = warnings,
                UsedProvider = usedProvider,
                UsedFallback = usedFallback,
                Source = "vision",
                DurationMs = durationMs,
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse vision OCR JSON response");
            warnings.Add("vision_json_parse_error");

            return new DocumentOcrResult
            {
                FullText = rawContent.Trim(),
                Confidence = 0.4m,
                Warnings = warnings,
                UsedProvider = usedProvider,
                UsedFallback = usedFallback,
                Source = "vision",
                DurationMs = durationMs,
            };
        }
    }

    // ── Shared Helpers ──────────────────────────────────────────────────────

    private sealed record ProviderSettings(string ApiKey, string? BaseUrl, string? ModelDefault);

    private async Task<ProviderSettings> GetRequiredProviderSettingsAsync(AiProvider provider, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var config = await db.AiProviderConfigs
            .FirstOrDefaultAsync(c => c.Provider == provider && c.IsActive, ct)
            ?? throw new InvalidOperationException($"No active API key found for vision provider {provider}.");

        return new ProviderSettings(
            _encryption.Decrypt(config.EncryptedApiKey),
            config.BaseUrl,
            config.ModelDefault);
    }

    private async Task<AiPrompt?> LoadPromptAsync(CancellationToken ct)
    {
        var cacheKey = $"ai:prompt:{PromptKey}";
        var cached = await _cache.GetAsync<AiPrompt>(cacheKey, ct);
        if (cached is not null) return cached;

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var prompt = await db.AiPrompts
            .FirstOrDefaultAsync(p => p.PromptKey == PromptKey && p.IsActive, ct);

        if (prompt is not null)
            await _cache.SetAsync(cacheKey, prompt, PromptCacheTtl, ct);

        return prompt;
    }

    private async Task WriteLogAsync(
        Guid promptId, AiProvider provider, bool usedFallback,
        int inputTokens, int outputTokens, int durationMs,
        bool isSuccess, string? errorMessage, Guid entityId, CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var log = AiCallLog.Create(
                promptId, provider, usedFallback,
                inputTokens, outputTokens, durationMs,
                isSuccess, errorMessage,
                null, entityId); // background context — no user

            db.AiCallLogs.Add(log);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write AiCallLog for vision prompt {PromptId}", promptId);
        }
    }

    private static string ExtractContent(JsonElement root)
    {
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content");
        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join("\n", content.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => item.TryGetProperty("text", out var text) ? text.GetString() : null)
                .Where(text => !string.IsNullOrWhiteSpace(text))),
            _ => string.Empty,
        };
    }

    private static string? ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                var lastFence = trimmed.LastIndexOf("```");
                if (lastFence > firstNewline)
                    trimmed = trimmed[(firstNewline + 1)..lastFence].Trim();
            }
        }

        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            return trimmed[firstBrace..(lastBrace + 1)];

        return null;
    }

    private static string? GetStringProp(JsonElement el, string name)
        => el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static decimal? GetDecimalProp(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetDecimal() : null;
    }

    private static int GetInt32OrDefault(JsonElement root, string section, string property)
    {
        if (!root.TryGetProperty(section, out var sectionEl) || sectionEl.ValueKind != JsonValueKind.Object)
            return 0;
        return sectionEl.TryGetProperty(property, out var prop) ? prop.GetInt32() : 0;
    }

    private static string ResolveEndpoint(string? configuredBaseUrl, string defaultUrl, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
            return defaultUrl;

        var trimmedBaseUrl = configuredBaseUrl.TrimEnd('/');
        var relativeSegments = relativePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (!Uri.TryCreate(trimmedBaseUrl, UriKind.Absolute, out var configuredUri))
            return trimmedBaseUrl;

        var baseSegments = configuredUri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var overlap = GetPathOverlap(baseSegments, relativeSegments);
        var remainder = string.Join('/', relativeSegments.Skip(overlap));

        return string.IsNullOrWhiteSpace(remainder)
            ? trimmedBaseUrl
            : $"{trimmedBaseUrl}/{remainder}";
    }

    private static int GetPathOverlap(string[] baseSegments, string[] relativeSegments)
    {
        for (var count = Math.Min(baseSegments.Length, relativeSegments.Length); count > 0; count--)
        {
            var matches = true;
            for (var i = 0; i < count; i++)
            {
                if (!string.Equals(
                        baseSegments[baseSegments.Length - count + i],
                        relativeSegments[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return count;
        }

        return 0;
    }

    private static string GetDefaultModel(AiProvider provider) => provider switch
    {
        AiProvider.Gemini => "gemini-2.5-flash",
        AiProvider.OpenAI => "gpt-4o-mini",
        AiProvider.ZAI    => "glm-4.6v-flash",
        _ => "unknown",
    };

    private static readonly JsonSerializerOptions s_snakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
