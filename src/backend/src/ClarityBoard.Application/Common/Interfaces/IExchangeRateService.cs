namespace ClarityBoard.Application.Common.Interfaces;

public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate for the given currency relative to EUR for the specified date.
    /// Returns null if the rate is not available.
    /// </summary>
    Task<decimal?> GetRateAsync(string currency, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Gets all available exchange rates relative to EUR for the specified date.
    /// Keys are ISO 4217 currency codes (e.g., "USD", "GBP").
    /// </summary>
    Task<Dictionary<string, decimal>> GetRatesAsync(DateOnly date, CancellationToken ct = default);
}
