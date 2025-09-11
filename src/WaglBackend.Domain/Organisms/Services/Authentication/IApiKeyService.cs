using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Domain.Organisms.Services.Authentication;

public interface IApiKeyService
{
    Task<Provider?> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<ApiKey> GenerateApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<bool> RevokeApiKeyAsync(Guid providerId, CancellationToken cancellationToken = default);
    Task<bool> IsApiKeyValidAsync(string apiKey, CancellationToken cancellationToken = default);
    Task UpdateLastAccessedAsync(Guid providerId, CancellationToken cancellationToken = default);
}