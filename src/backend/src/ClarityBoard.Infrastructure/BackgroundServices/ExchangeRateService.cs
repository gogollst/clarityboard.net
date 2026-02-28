using System.Globalization;
using System.Xml.Linq;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ClarityBoard.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that runs daily at 06:00 UTC.
/// Fetches ECB exchange rates and stores them in Redis cache.
/// Implements IExchangeRateService for on-demand rate queries.
/// </summary>
public sealed class ExchangeRateService : BackgroundService, IExchangeRateService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateService> _logger;

    private const string EcbDailyUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    private const string RedisKeyPrefix = "exchange_rate:";
    private const string RedisDateKey = "exchange_rate:_last_date";
    private static readonly TimeSpan RateTtl = TimeSpan.FromHours(48);

    public ExchangeRateService(
        IConnectionMultiplexer redis,
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeRateService> logger)
    {
        _redis = redis;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExchangeRateService started");

        // Run an initial fetch on startup
        await FetchAndCacheRatesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNext0600Utc();
            _logger.LogInformation(
                "ExchangeRateService sleeping until {NextRun} ({Delay})",
                DateTime.UtcNow.Add(delay),
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await FetchAndCacheRatesAsync(stoppingToken);
        }

        _logger.LogInformation("ExchangeRateService stopped");
    }

    internal static TimeSpan CalculateDelayUntilNext0600Utc()
    {
        var now = DateTime.UtcNow;
        var today0600 = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0, DateTimeKind.Utc);

        // If we are past 06:00 today, schedule for tomorrow
        var next = now < today0600 ? today0600 : today0600.AddDays(1);
        return next - now;
    }

    internal async Task FetchAndCacheRatesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Fetching ECB exchange rates");

        try
        {
            var client = _httpClientFactory.CreateClient("ecb");
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetStringAsync(EcbDailyUrl, ct);
            var rates = ParseEcbXml(response);

            if (rates.Count == 0)
            {
                _logger.LogWarning("ECB response contained no exchange rates");
                return;
            }

            var db = _redis.GetDatabase();
            var batch = db.CreateBatch();
            var tasks = new List<Task>();

            foreach (var (currency, rate) in rates)
            {
                var key = $"{RedisKeyPrefix}{currency}";
                tasks.Add(batch.StringSetAsync(key, rate.ToString(CultureInfo.InvariantCulture), RateTtl));
            }

            // Store the date of the last successful fetch
            tasks.Add(batch.StringSetAsync(RedisDateKey, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), RateTtl));

            batch.Execute();
            await Task.WhenAll(tasks);

            _logger.LogInformation("Cached {Count} exchange rates from ECB", rates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ECB exchange rates. Previous rates will be retained.");
            // Fallback: keep previous day's rates (they persist in Redis with TTL)
        }
    }

    internal static Dictionary<string, decimal> ParseEcbXml(string xml)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace gesmes = "http://www.gesmes.org/xml/2002-08-01";
            XNamespace ecb = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

            var cubeElements = doc.Descendants(ecb + "Cube")
                .Where(e => e.Attribute("currency") != null);

            foreach (var cube in cubeElements)
            {
                var currency = cube.Attribute("currency")?.Value;
                var rateStr = cube.Attribute("rate")?.Value;

                if (currency is not null && rateStr is not null
                    && decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                {
                    rates[currency] = rate;
                }
            }
        }
        catch (Exception)
        {
            // Return empty dictionary on parse failure
        }

        return rates;
    }

    // ── IExchangeRateService implementation ─────────────────────────────

    public async Task<decimal?> GetRateAsync(string currency, DateOnly date, CancellationToken ct = default)
    {
        if (string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{RedisKeyPrefix}{currency.ToUpperInvariant()}";
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                return rate;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve exchange rate for {Currency}", currency);
            return null;
        }
    }

    public async Task<Dictionary<string, decimal>> GetRatesAsync(DateOnly date, CancellationToken ct = default)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var db = _redis.GetDatabase();
            var endpoints = _redis.GetEndPoints();

            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.Keys(pattern: $"clarityboard:{RedisKeyPrefix}*").ToArray();

                foreach (var key in keys)
                {
                    var keyStr = key.ToString();
                    // Extract currency code from "clarityboard:exchange_rate:USD"
                    var currency = keyStr.Split(':').LastOrDefault();
                    if (currency is null || currency.StartsWith('_'))
                        continue;

                    var value = await db.StringGetAsync(key);
                    if (!value.IsNullOrEmpty
                        && decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                    {
                        rates[currency] = rate;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve exchange rates from Redis");
        }

        return rates;
    }
}
