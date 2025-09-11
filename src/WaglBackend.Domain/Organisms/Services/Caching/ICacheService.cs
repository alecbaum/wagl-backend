namespace WaglBackend.Domain.Organisms.Services.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<long> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    Task<bool> SetAsync<T>(string key, T value, DateTime absoluteExpiration, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetExpirationAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> RefreshAsync(string key, CancellationToken cancellationToken = default);
    Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    Task<bool> SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}