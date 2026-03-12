using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Translation;

public class DeepLTranslationService : ITranslationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly ILogger<DeepLTranslationService> _logger;

    private static readonly Dictionary<string, string> LangMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "DE",
        ["en"] = "EN",
        ["ru"] = "RU",
    };

    public DeepLTranslationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DeepLTranslationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _baseUrl = configuration["AI:DeepL:BaseUrl"] ?? "https://api-free.deepl.com";
        _apiKey = configuration["AI:DeepL:ApiKey"];
    }

    public async Task<Dictionary<string, string>> TranslateAsync(
        string text,
        string sourceLanguage,
        IEnumerable<string> targetLanguages,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("DeepL API key not configured — skipping translation");
            return result;
        }

        if (string.IsNullOrWhiteSpace(text))
            return result;

        if (!LangMap.TryGetValue(sourceLanguage, out var deepLSource))
        {
            _logger.LogWarning("Unsupported source language: {Lang}", sourceLanguage);
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
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/translate");
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");
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
                _logger.LogWarning(ex, "DeepL translation failed for {Source}→{Target}", sourceLanguage, targetLang);
            }
        }

        return result;
    }

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
