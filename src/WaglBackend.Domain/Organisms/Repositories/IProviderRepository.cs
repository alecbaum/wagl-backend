using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IProviderRepository : IRepository<Provider>
{
    Task<Provider?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<Provider?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> UpdateLastAccessedAsync(Guid providerId, DateTime lastAccessedAt, CancellationToken cancellationToken = default);
    Task<IEnumerable<Provider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Provider>> GetProvidersByLastAccessAsync(DateTime since, CancellationToken cancellationToken = default);
}