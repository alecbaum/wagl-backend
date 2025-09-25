using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Repositories;

public class ProviderRepository : BaseRepository<Provider>, IProviderRepository
{
    public ProviderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Provider?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(p => p.ApiKey.Value == apiKey && p.IsActive, cancellationToken);
    }

    public async Task<Provider?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<bool> UpdateLastAccessedAsync(Guid providerId, DateTime lastAccessedAt, CancellationToken cancellationToken = default)
    {
        var provider = await GetByIdAsync(providerId, cancellationToken);
        if (provider == null)
            return false;

        provider.LastAccessedAt = lastAccessedAt;
        await UpdateAsync(provider, cancellationToken);
        return true;
    }

    public async Task<IEnumerable<Provider>> GetActiveProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Provider>> GetProvidersByLastAccessAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(p => p.LastAccessedAt >= since)
            .OrderByDescending(p => p.LastAccessedAt)
            .ToListAsync(cancellationToken);
    }
}