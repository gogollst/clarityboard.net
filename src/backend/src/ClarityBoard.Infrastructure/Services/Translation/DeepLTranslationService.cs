using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Translation;

public class DeepLTranslationService : ITranslationService
{
    private readonly IServiceProvider _sp;
    private readonly IEncryptionService _encryption;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeepLTranslationService> _logger;

    private const string DefaultBaseUrl = "https://api-free.deepl.com";

    private static readonly Dictionary<string, string> LangMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "DE",
        ["en"] = "EN",
        ["ru"] = "RU",
    };

    public DeepLTranslationService(
        IServiceProvider sp,
        IEncryptionService encryption,
        IHttpClientFactory httpClientFactory,
        ILogger<DeepLTranslationService> logger)
    {
        _sp = sp;
        _encryption = encryption;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> TranslateAsync(
        string text,
        string sourceLanguage,
        IEnumerable<string> targetLanguages,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(text))
            return result;

        if (!LangMap.TryGetValue(sourceLanguage, out var deepLSource))
        {
            _logger.LogWarning("Unsupported source language: {Lang}", sourceLanguage);
            return result;
        }

        // Load API key from DB (same pattern as PromptAiService)
        var settings = await GetDeepLSettingsAsync(ct);
        if (settings is null)
        {
            _logger.LogDebug("No active DeepL provider config found — skipping translation");
            return result;
        }

        var client = _httpClientFactory.CreateClient("deepl");

        foreach (var targetLang in targetLanguages)
        {
            if (targetLang.Equals(sourceLanguage, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!LangMap.TryGetValue(targetLang, out var deepLTarget))
                continue;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl}/v2/translate");
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {settings.ApiKey}");
                request.Content = JsonContent.Create(new
                {
                    text = new[] { text },
                    source_lang = deepLSource,
                    target_lang = deepLTarget,
                });

                var response = await client.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadFromJsonAsync<DeepLResponse>(ct);
                if (body?.Translations is { Count: > 0 })
                {
                    result[targetLang] = body.Translations[0].Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeepL translation failed for {Source}->{Target}", sourceLanguage, targetLang);
            }
        }

        return result;
    }

    private async Task<DeepLSettings?> GetDeepLSettingsAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var config = await db.AiProviderConfigs
            .FirstOrDefaultAsync(c => c.Provider == AiProvider.DeepL && c.IsActive, ct);

        if (config is null)
            return null;

        return new DeepLSettings(
            _encryption.Decrypt(config.EncryptedApiKey),
            config.BaseUrl ?? DefaultBaseUrl);
    }

    private sealed record DeepLSettings(string ApiKey, string BaseUrl);

    private sealed class DeepLResponse
    {
        [JsonPropertyName("translations")]
        public List<DeepLTranslation> Translations { get; set; } = [];
    }

    private sealed class DeepLTranslation
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;
    }
}
