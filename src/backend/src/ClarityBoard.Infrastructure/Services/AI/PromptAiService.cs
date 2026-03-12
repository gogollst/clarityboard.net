using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.AI;

/// <summary>
/// Central AI execution engine. Loads prompts by key, replaces template variables,
/// routes to the configured provider with automatic fallback, and writes AiCallLog entries.
/// </summary>
public sealed class PromptAiService : IPromptAiService
{
    private readonly IServiceProvider _sp;
    private readonly ICacheService _cache;
    private readonly IEncryptionService _encryption;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PromptAiService> _logger;

    private static readonly TimeSpan PromptCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly Regex ConditionalBlockRegex = new(@"{{#if ([^}]+)}}(.*?){{/if}}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex PlaceholderRegex = new(@"{{[^}]+}}", RegexOptions.Compiled);

    // Anthropic constants
    private const string AnthropicUrl     = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const string AnthropicRelativePath = "v1/messages";

    // OpenAI constants
    private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";
    private const string OpenAiRelativePath = "v1/chat/completions";
    private const string GrokUrl = "https://api.x.ai/v1/chat/completions";
    private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    private const string GeminiRelativePath = "v1beta/openai/chat/completions";
    private const string ZaiUrl = "https://api.z.ai/api/paas/v4/chat/completions";
    private const string ZaiRelativePath = "api/paas/v4/chat/completions";
    private const string ManusUrl = "https://api.manus.im/v1/chat/completions";

    public PromptAiService(
        IServiceProvider sp,
        ICacheService cache,
        IEncryptionService encryption,
        ICurrentUser currentUser,
        IHttpClientFactory httpClientFactory,
        ILogger<PromptAiService> logger)
    {
        _sp                = sp;
        _cache             = cache;
        _encryption        = encryption;
        _currentUser       = currentUser;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public async Task<AiResponse> ExecuteAsync(
        string promptKey,
        Dictionary<string, string> variables,
        CancellationToken ct)
    {
        var prompt = await LoadPromptAsync(promptKey, ct)
            ?? throw new KeyNotFoundException($"AI prompt '{promptKey}' not found or inactive.");

        var systemText = prompt.SystemPrompt;
        var userText   = ReplaceVariables(prompt.UserPromptTemplate ?? string.Empty, variables);

        var sw          = Stopwatch.StartNew();
        bool usedFallback = false;
        AiProvider usedProvider;
        AiCallResult? result = null;
        string? errorMessage = null;

        // Try primary provider
        try
        {
            result      = await CallProviderAsync(prompt.PrimaryProvider, prompt.PrimaryModel, systemText, userText, prompt.Temperature, prompt.MaxTokens, ct);
            usedProvider = prompt.PrimaryProvider;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary provider {Provider} failed for prompt '{Key}'. Trying fallback.", prompt.PrimaryProvider, promptKey);
            errorMessage = ex.Message;

            // Try fallback
            try
            {
                result       = await CallProviderAsync(prompt.FallbackProvider, prompt.FallbackModel, systemText, userText, prompt.Temperature, prompt.MaxTokens, ct);
                usedProvider = prompt.FallbackProvider;
                usedFallback = true;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback provider {Provider} also failed for prompt '{Key}'.", prompt.FallbackProvider, promptKey);
                sw.Stop();
                await WriteLogAsync(prompt.Id, prompt.FallbackProvider, true, 0, 0, (int)sw.ElapsedMilliseconds, false, fallbackEx.Message, ct);
                throw;
            }
        }

        sw.Stop();
        await WriteLogAsync(prompt.Id, usedProvider, usedFallback, result.InputTokens, result.OutputTokens, (int)sw.ElapsedMilliseconds, true, errorMessage, ct);

        return new AiResponse
        {
            Content      = result.Content,
            UsedProvider = usedProvider,
            UsedFallback = usedFallback,
            InputTokens  = result.InputTokens,
            OutputTokens = result.OutputTokens,
            DurationMs   = (int)sw.ElapsedMilliseconds,
        };
    }

    public async Task<string> EnhancePromptAsync(
        string currentSystemPrompt, string? userTemplate,
        string description, string functionDescription, CancellationToken ct)
    {
        var vars = new Dictionary<string, string>
        {
            ["current_system_prompt"]  = currentSystemPrompt,
            ["user_template"]          = userTemplate ?? string.Empty,
            ["description"]            = description,
            ["function_description"]   = functionDescription,
        };

        var response = await ExecuteAsync("system.enhance_prompt", vars, ct);
        return response.Content;
    }

    public async Task<bool> TestProviderAsync(AiProvider provider, CancellationToken ct)
    {
        try
        {
            var settings = await TryGetProviderSettingsAsync(provider, ct);
            if (settings is null) return false;

            var result = await CallProviderAsync(
                provider, settings.ModelDefault ?? GetDefaultModel(provider),
                "You are a health check bot.", "Respond with exactly: OK",
                temperature: 0m, maxTokens: 50, ct, settings.ApiKey);

            return !string.IsNullOrWhiteSpace(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for provider {Provider}", provider);
            return false;
        }
    }


    // ── Private Helpers ───────────────────────────────────────────────────

    private sealed record AiCallResult(string Content, int InputTokens, int OutputTokens);
    private sealed record ProviderRuntimeSettings(string ApiKey, string? BaseUrl, string? ModelDefault);

    private async Task<AiCallResult> CallProviderAsync(
        AiProvider provider, string model,
        string systemPrompt, string userMessage,
        decimal temperature, int maxTokens,
        CancellationToken ct, string? overrideKey = null)
    {
        var settings = await GetRequiredProviderSettingsAsync(provider, overrideKey, ct);
        var modelToUse = string.IsNullOrWhiteSpace(model)
            ? settings.ModelDefault ?? GetDefaultModel(provider)
            : model;

        return provider switch
        {
            AiProvider.Anthropic => await CallAnthropicAsync(settings.ApiKey, ResolveEndpoint(settings.BaseUrl, AnthropicUrl, AnthropicRelativePath), modelToUse, systemPrompt, userMessage, temperature, maxTokens, ct),
            AiProvider.OpenAI    => await CallOpenAiAsync(settings.ApiKey, ResolveEndpoint(settings.BaseUrl, OpenAiUrl, OpenAiRelativePath), modelToUse, systemPrompt, userMessage, temperature, maxTokens, ct),
            AiProvider.Grok      => await CallOpenAiCompatibleAsync(settings.ApiKey, ResolveEndpoint(settings.BaseUrl, GrokUrl, OpenAiRelativePath), modelToUse, systemPrompt, userMessage, temperature, maxTokens, ct),
            AiProvider.Gemini    => await CallOpenAiCompatibleAsync(settings.ApiKey, ResolveEndpoint(settings.BaseUrl, GeminiUrl, GeminiRelativePath), modelToUse, systemPrompt, userMessage, temperature, maxTokens, ct),
            AiProvider.ZAI       => await CallOpenAiCompatibleAsync(settings.ApiKey, ResolveEndpoint(settings.BaseUrl, ZaiUrl, ZaiRelativePath), modelToUse, systemPrompt, userMessage, temperature, maxTokens, ct),
            AiProvider.Manus     => await CallOpenAiCompatibleAsync(settings.ApiKey, ResolveEndpoint(settings.BaseUrl, ManusUrl, OpenAiRelativePath), modelToUse, systemPrompt, userMessage, temperature, maxTokens, ct),
            _                    => throw new NotSupportedException($"Provider {provider} is not supported."),
        };
    }

    private async Task<AiCallResult> CallAnthropicAsync(
        string apiKey, string url, string model, string systemPrompt, string userMessage,
        decimal temperature, int maxTokens, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient("ai_prompt");
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
        client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", AnthropicVersion);

        var body = JsonSerializer.Serialize(new
        {
            model,
            max_tokens  = maxTokens,
            temperature = (double)temperature,
            system      = systemPrompt,
            messages    = new[] { new { role = "user", content = userMessage } },
        }, SnakeCaseOptions);

        using var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
        using var response    = await client.PostAsync(url, httpContent, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Anthropic API {response.StatusCode}: {err}");
        }

        using var doc  = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root       = doc.RootElement;
        var text       = root.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        var inputTok   = root.GetProperty("usage").GetProperty("input_tokens").GetInt32();
        var outputTok  = root.GetProperty("usage").GetProperty("output_tokens").GetInt32();

        return new AiCallResult(text, inputTok, outputTok);
    }

    private async Task<AiCallResult> CallOpenAiAsync(
        string apiKey, string url, string model, string systemPrompt, string userMessage,
        decimal temperature, int maxTokens, CancellationToken ct)
        => await CallOpenAiCompatibleAsync(apiKey, url, model, systemPrompt, userMessage, temperature, maxTokens, ct);

    private async Task<AiCallResult> CallOpenAiCompatibleAsync(
        string apiKey, string url, string model, string systemPrompt, string userMessage,
        decimal temperature, int maxTokens, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient("ai_prompt");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var body = JsonSerializer.Serialize(new
        {
            model,
            max_tokens  = maxTokens,
            temperature = (double)temperature,
            messages    = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  },
            },
        }, SnakeCaseOptions);

        using var httpContent = new StringContent(body, Encoding.UTF8, "application/json");
        using var response    = await client.PostAsync(url, httpContent, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"OpenAI-compatible API {response.StatusCode}: {err}");
        }

        using var doc  = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root       = doc.RootElement;
        var text       = ExtractOpenAiCompatibleContent(root);
        var usage      = root.TryGetProperty("usage", out var usageElement) ? usageElement : default;
        var inputTok   = GetInt32OrDefault(usage, "prompt_tokens");
        var outputTok  = GetInt32OrDefault(usage, "completion_tokens");

        return new AiCallResult(text, inputTok, outputTok);
    }

    private async Task<ProviderRuntimeSettings?> TryGetProviderSettingsAsync(AiProvider provider, CancellationToken ct)
    {
        using var scope  = _sp.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IAppDbContext>();
        var config       = await db.AiProviderConfigs
            .FirstOrDefaultAsync(c => c.Provider == provider && c.IsActive, ct);

        if (config is null) return null;

        return new ProviderRuntimeSettings(
            _encryption.Decrypt(config.EncryptedApiKey),
            config.BaseUrl,
            config.ModelDefault);
    }

    private async Task<ProviderRuntimeSettings> GetRequiredProviderSettingsAsync(
        AiProvider provider,
        string? overrideKey,
        CancellationToken ct)
    {
        var settings = await TryGetProviderSettingsAsync(provider, ct);
        var apiKey = overrideKey ?? settings?.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"No active API key found for provider {provider}.");

        return new ProviderRuntimeSettings(apiKey, settings?.BaseUrl, settings?.ModelDefault);
    }

    private async Task<Domain.Entities.AI.AiPrompt?> LoadPromptAsync(string promptKey, CancellationToken ct)
    {
        var cacheKey = $"ai:prompt:{promptKey}";
        var cached   = await _cache.GetAsync<Domain.Entities.AI.AiPrompt>(cacheKey, ct);
        if (cached is not null) return cached;

        using var scope = _sp.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IAppDbContext>();
        var prompt      = await db.AiPrompts
            .FirstOrDefaultAsync(p => p.PromptKey == promptKey && p.IsActive, ct);

        if (prompt is not null)
            await _cache.SetAsync(cacheKey, prompt, PromptCacheTtl, ct);

        return prompt;
    }

    private async Task WriteLogAsync(
        Guid promptId, AiProvider provider, bool usedFallback,
        int inputTokens, int outputTokens, int durationMs,
        bool isSuccess, string? errorMessage, CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IAppDbContext>();

            Guid? userId   = null;
            Guid? entityId = null;
            try { userId   = _currentUser.UserId;   } catch { /* background service context */ }
            try { entityId = _currentUser.EntityId; } catch { /* background service context */ }

            var log = AiCallLog.Create(
                promptId, provider, usedFallback,
                inputTokens, outputTokens, durationMs, isSuccess, errorMessage,
                userId, entityId);

            db.AiCallLogs.Add(log);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write AiCallLog for prompt {PromptId}", promptId);
        }
    }

    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var rendered = ConditionalBlockRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return variables.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? match.Groups[2].Value
                : string.Empty;
        });

        var sb = new StringBuilder(rendered);
        foreach (var (key, value) in variables)
            sb.Replace($"{{{{{key}}}}}", value);

        return PlaceholderRegex.Replace(sb.ToString(), string.Empty).Trim();
    }

    private static string ExtractOpenAiCompatibleContent(JsonElement root)
    {
        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content");

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join("\n", content.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => item.TryGetProperty("text", out var text) ? text.GetString() : null)
                .Where(text => !string.IsNullOrWhiteSpace(text))),
            JsonValueKind.Object when content.TryGetProperty("text", out var text) => text.GetString() ?? string.Empty,
            _ => string.Empty,
        };
    }

    private static int GetInt32OrDefault(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property.GetInt32()
            : 0;

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
        AiProvider.Anthropic => "claude-sonnet-4-20250514",
        AiProvider.OpenAI    => "gpt-4o-mini",
        AiProvider.Grok      => "grok-3-mini",
        AiProvider.Gemini    => "gemini-2.5-flash",
        AiProvider.ZAI       => "glm-5",
        AiProvider.Manus     => "manus-default",
        _                    => "unknown",
    };

    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy          = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition        = JsonIgnoreCondition.WhenWritingNull,
    };
}

