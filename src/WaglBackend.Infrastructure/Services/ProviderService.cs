using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Core.Molecules.Exceptions;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Domain.Organisms.Services.Authentication;
using WaglBackend.Domain.Organisms.Services.Caching;

namespace WaglBackend.Infrastructure.Services;

public class ProviderService : IProviderService
{
    private readonly IProviderRepository _providerRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(
        IProviderRepository providerRepository,
        IApiKeyService apiKeyService,
        ICacheService cacheService,
        ILogger<ProviderService> logger)
    {
        _providerRepository = providerRepository;
        _apiKeyService = apiKeyService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ProviderResponse> CreateProviderAsync(CreateProviderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.Name))
                throw new BusinessRuleException("InvalidProviderName", "Provider name is required");

            if (string.IsNullOrEmpty(request.ContactEmail))
                throw new BusinessRuleException("InvalidEmail", "Contact email is required");

            // Check if provider with this email already exists (use GetAllAsync to filter by email for now)
            var allProviders = await _providerRepository.GetAllAsync(cancellationToken);
            var existingProvider = allProviders.FirstOrDefault(p => p.ContactEmail == request.ContactEmail);
            if (existingProvider != null)
            {
                throw new BusinessRuleException("ProviderAlreadyExists", $"Provider with email {request.ContactEmail} already exists");
            }

            // Generate API key
            var apiKey = ApiKey.Create();

            // Create provider entity
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                ContactEmail = request.ContactEmail,
                Description = request.Description,
                ApiKey = apiKey,
                AllowedIpAddresses = request.AllowedIpAddresses,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = null
            };

            // Save to repository
            var savedProvider = await _providerRepository.AddAsync(provider, cancellationToken);

            _logger.LogInformation("Created provider {ProviderId} with name {ProviderName}",
                savedProvider.Id, savedProvider.Name);

            // Return response with the unhashed API key (only time it's visible)
            return new ProviderResponse
            {
                Id = savedProvider.Id,
                Name = savedProvider.Name,
                ContactEmail = savedProvider.ContactEmail,
                Description = savedProvider.Description,
                ApiKeyPreview = apiKey.Value, // Return the original key, not the hash
                AllowedIpAddresses = savedProvider.AllowedIpAddresses,
                IsActive = savedProvider.IsActive,
                CreatedAt = savedProvider.CreatedAt,
                LastAccessedAt = savedProvider.LastAccessedAt
            };
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogError(ex, "Error creating provider");
            throw new BusinessRuleException("ProviderCreationFailed", "Failed to create provider");
        }
    }

    public async Task<ProviderResponse?> GetProviderByIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
                return null;

            return MapToProviderResponse(provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider by ID: {ProviderId}", providerId);
            throw;
        }
    }

    public async Task<IEnumerable<ProviderResponse>> GetAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var providers = await _providerRepository.GetAllAsync(cancellationToken);
            return providers.Select(MapToProviderResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all providers");
            throw;
        }
    }

    public async Task<string> RegenerateApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
            {
                throw new BusinessRuleException("ProviderNotFound", $"Provider with ID {providerId} not found");
            }

            // Generate new API key
            var newApiKey = ApiKey.Create();

            // Update provider
            provider.ApiKey = newApiKey;
            provider.LastAccessedAt = null; // Reset last used since key changed

            await _providerRepository.UpdateAsync(provider, cancellationToken);

            // Invalidate cache for the old key
            await _cacheService.RemoveAsync($"provider_validation_{provider.ApiKey.Value}", cancellationToken);

            _logger.LogInformation("Regenerated API key for provider {ProviderId}", providerId);

            return newApiKey.Value;
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogError(ex, "Error regenerating API key for provider: {ProviderId}", providerId);
            throw new BusinessRuleException("ApiKeyRegenerationFailed", "Failed to regenerate API key");
        }
    }

    public async Task<ProviderResponse> UpdateProviderAsync(Guid providerId, CreateProviderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
            {
                throw new BusinessRuleException("ProviderNotFound", $"Provider with ID {providerId} not found");
            }

            // Update fields
            if (!string.IsNullOrEmpty(request.Name))
                provider.Name = request.Name;

            if (!string.IsNullOrEmpty(request.ContactEmail))
            {
                // Check if email is already used by another provider
                var allProviders = await _providerRepository.GetAllAsync(cancellationToken);
                var existingProvider = allProviders.FirstOrDefault(p => p.ContactEmail == request.ContactEmail);
                if (existingProvider != null && existingProvider.Id != providerId)
                {
                    throw new BusinessRuleException("EmailAlreadyUsed", $"Email {request.ContactEmail} is already used by another provider");
                }
                provider.ContactEmail = request.ContactEmail;
            }

            if (request.AllowedIpAddresses != null)
                provider.AllowedIpAddresses = request.AllowedIpAddresses.ToArray();

            await _providerRepository.UpdateAsync(provider, cancellationToken);

            _logger.LogInformation("Updated provider {ProviderId}", providerId);

            return MapToProviderResponse(provider);
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogError(ex, "Error updating provider: {ProviderId}", providerId);
            throw new BusinessRuleException("ProviderUpdateFailed", "Failed to update provider");
        }
    }

    public async Task<bool> DeactivateProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
                return false;

            provider.IsActive = false;
            await _providerRepository.UpdateAsync(provider, cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync($"provider_validation_{provider.ApiKey.Value}", cancellationToken);

            _logger.LogInformation("Deactivated provider {ProviderId}", providerId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating provider: {ProviderId}", providerId);
            throw;
        }
    }

    public async Task<ProviderResponse?> ValidateProviderByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"provider_validation_{apiKey}";
            var cachedProvider = await _cacheService.GetAsync<ProviderResponse>(cacheKey, cancellationToken);
            if (cachedProvider != null)
                return cachedProvider;

            // Create temporary ApiKey for comparison
            var tempApiKey = ApiKey.FromString(apiKey);
            // Find provider by checking hashed value against stored keys
            var allProviders = await _providerRepository.GetAllAsync(cancellationToken);
            var provider = allProviders.FirstOrDefault(p => p.ApiKey?.Verify(apiKey) == true);

            if (provider == null || !provider.IsActive)
                return null;

            // Update last used timestamp
            provider.LastAccessedAt = DateTime.UtcNow;
            await _providerRepository.UpdateAsync(provider, cancellationToken);

            var response = MapToProviderResponse(provider);

            // Cache for 5 minutes
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating provider by API key");
            return null;
        }
    }

    private static ProviderResponse MapToProviderResponse(Provider provider)
    {
        return new ProviderResponse
        {
            Id = provider.Id,
            Name = provider.Name,
            ContactEmail = provider.ContactEmail,
            ApiKeyPreview = "***HIDDEN***", // Don't expose the actual key in normal responses
            AllowedIpAddresses = provider.AllowedIpAddresses ?? new string[0],
            IsActive = provider.IsActive,
            CreatedAt = provider.CreatedAt,
            LastAccessedAt = provider.LastAccessedAt
        };
    }
}