namespace ClarityBoard.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public static class CacheTtl
{
    public static readonly TimeSpan Kpi = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Accounts = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Entity = TimeSpan.FromHours(1);
    public static readonly TimeSpan KpiDefinitions = TimeSpan.FromHours(24);
    public static readonly TimeSpan Permissions = TimeSpan.FromMinutes(15);
}
