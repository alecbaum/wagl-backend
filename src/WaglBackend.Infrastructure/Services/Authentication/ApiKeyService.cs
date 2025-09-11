using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.Exceptions;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Domain.Organisms.Services.Caching;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Services.Authentication;

public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        ApplicationDbContext context,
        ICacheService cacheService,
        ILogger<ApiKeyService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Provider?> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first for performance
            var cacheKey = CacheKeys.ApiKey(apiKey.GetHashCode().ToString());
            var cachedProvider = await _cacheService.GetAsync<Provider>(cacheKey, cancellationToken);

            if (cachedProvider != null)
            {
                _logger.LogDebug("API key validation cache hit for provider {ProviderId}", cachedProvider.Id);
                
                // Update last accessed time asynchronously
                _ = Task.Run(async () => await UpdateLastAccessedAsync(cachedProvider.Id, cancellationToken), cancellationToken);
                
                return cachedProvider;
            }

            // Query database if not in cache
            var provider = await _context.Providers
                .Where(p => p.IsActive && p.ApiKey != null)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApiKey!.Value == apiKey, cancellationToken);

            if (provider != null && provider.ApiKey != null)
            {
                // Verify the API key hash
                if (provider.ApiKey.Verify(apiKey))
                {
                    // Cache the valid provider for 5 minutes
                    await _cacheService.SetAsync(cacheKey, provider, TimeSpan.FromMinutes(5), cancellationToken);
                    
                    // Update last accessed time
                    await UpdateLastAccessedAsync(provider.Id, cancellationToken);
                    
                    _logger.LogDebug("API key validation successful for provider {ProviderId}", provider.Id);
                    return provider;
                }
                else
                {
                    _logger.LogWarning("API key hash verification failed for provider {ProviderId}", provider.Id);
                }
            }

            _logger.LogWarning("API key validation failed. Invalid or inactive API key provided");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            throw;
        }
    }

    public async Task<ApiKey> GenerateApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

            if (provider == null)
            {
                throw new InvalidOperationException($"Provider with ID {providerId} not found");
            }

            if (!provider.IsActive)
            {
                throw new InvalidOperationException($"Provider {providerId} is not active");
            }

            // Generate new API key
            var newApiKey = ApiKey.Create();
            provider.ApiKey = newApiKey;

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache for this provider
            await _cacheService.RemoveByPatternAsync($"{CacheKeys.ApiKeyPrefix}:*", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.Provider(providerId), cancellationToken);

            _logger.LogInformation("New API key generated for provider {ProviderId}", providerId);
            return newApiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key for provider {ProviderId}", providerId);
            throw;
        }
    }

    public async Task<bool> RevokeApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

            if (provider == null)
            {
                _logger.LogWarning("Attempted to revoke API key for non-existent provider {ProviderId}", providerId);
                return false;
            }

            // Remove the API key
            provider.ApiKey = null;
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate related caches
            await _cacheService.RemoveByPatternAsync($"{CacheKeys.ApiKeyPrefix}:*", cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.Provider(providerId), cancellationToken);

            _logger.LogInformation("API key revoked for provider {ProviderId}", providerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key for provider {ProviderId}", providerId);
            return false;
        }
    }

    public async Task<bool> IsApiKeyValidAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await ValidateApiKeyAsync(apiKey, cancellationToken);
            return provider != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking API key validity");
            return false;
        }
    }

    public async Task UpdateLastAccessedAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

            if (provider != null)
            {
                provider.LastAccessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                // Update cache if it exists
                var cacheKey = CacheKeys.Provider(providerId);
                if (await _cacheService.ExistsAsync(cacheKey, cancellationToken))
                {
                    await _cacheService.SetAsync(cacheKey, provider, TimeSpan.FromMinutes(5), cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last accessed time for provider {ProviderId}", providerId);
            // Don't throw here as this is a non-critical operation
        }
    }
}