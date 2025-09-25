using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

/// <summary>
/// Service for managing provider accounts and API keys
/// </summary>
public interface IProviderService
{
    /// <summary>
    /// Create a new provider account
    /// </summary>
    Task<ProviderResponse> CreateProviderAsync(CreateProviderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get provider by ID
    /// </summary>
    Task<ProviderResponse?> GetProviderByIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all providers (admin only)
    /// </summary>
    Task<IEnumerable<ProviderResponse>> GetAllProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerate API key for a provider
    /// </summary>
    Task<string> RegenerateApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update provider information
    /// </summary>
    Task<ProviderResponse> UpdateProviderAsync(Guid providerId, CreateProviderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a provider account
    /// </summary>
    Task<bool> DeactivateProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate provider by API key
    /// </summary>
    Task<ProviderResponse?> ValidateProviderByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}