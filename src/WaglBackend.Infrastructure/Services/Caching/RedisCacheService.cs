using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WaglBackend.Core.Molecules.Configurations;
using WaglBackend.Domain.Organisms.Services.Caching;

namespace WaglBackend.Infrastructure.Services.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger,
        IOptions<RedisConfiguration> config)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _config = config.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFullKey(key);
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache value for key: {Key}", key);
            return default;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFullKey(key);
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(_config.DefaultExpirationMinutes));
            }

            await _distributedCache.SetStringAsync(cacheKey, serializedValue, options, cancellationToken);
            
            _logger.LogDebug("Cache value set for key: {CacheKey} with expiration: {Expiration}", 
                cacheKey, options.AbsoluteExpirationRelativeToNow);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, DateTime absoluteExpiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFullKey(key);
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };

            await _distributedCache.SetStringAsync(cacheKey, serializedValue, options, cancellationToken);
            
            _logger.LogDebug("Cache value set for key: {CacheKey} with absolute expiration: {Expiration}", 
                cacheKey, absoluteExpiration);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFullKey(key);
            await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            
            _logger.LogDebug("Cache value removed for key: {CacheKey}", cacheKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFullKey(key);
            var cachedValue = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
            return !string.IsNullOrEmpty(cachedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: Pattern removal requires Redis-specific implementation
            // This is a simplified version - in production, you might want to use Redis SCAN commands
            _logger.LogWarning("Pattern removal is not fully implemented for distributed cache. Pattern: {Pattern}", pattern);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache values by pattern: {Pattern}", pattern);
            return false;
        }
    }

    public async Task<long> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: Prefix removal requires Redis-specific implementation
            // This is a simplified version - in production, you might want to use Redis SCAN commands
            _logger.LogWarning("Prefix removal is not fully implemented for distributed cache. Prefix: {Prefix}", prefix);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache values by prefix: {Prefix}", prefix);
            return 0;
        }
    }

    public async Task<TimeSpan?> GetExpirationAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: Getting expiration time requires Redis-specific implementation
            // IDistributedCache doesn't provide this functionality
            _logger.LogWarning("Getting expiration time is not supported by IDistributedCache for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiration for key: {Key}", key);
            return null;
        }
    }

    public async Task<bool> RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetFullKey(key);
            await _distributedCache.RefreshAsync(cacheKey, cancellationToken);
            
            _logger.LogDebug("Cache value refreshed for key: {CacheKey}", cacheKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache value for key: {Key}", key);
            return false;
        }
    }

    public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, T?>();
        
        try
        {
            var tasks = keys.Select(async key =>
            {
                var value = await GetAsync<T>(key, cancellationToken);
                return new KeyValuePair<string, T?>(key, value);
            });

            var results = await Task.WhenAll(tasks);
            
            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple cache values");
        }

        return result;
    }

    public async Task<bool> SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = keyValuePairs.Select(kvp => 
                SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));

            var results = await Task.WhenAll(tasks);
            
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple cache values");
            return false;
        }
    }

    private string GetFullKey(string key)
    {
        return $"{_config.KeyPrefix}{key}";
    }
}