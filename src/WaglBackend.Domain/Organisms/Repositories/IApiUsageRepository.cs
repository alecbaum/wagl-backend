using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IApiUsageRepository : IRepository<ApiUsageLog>
{
    Task<IEnumerable<ApiUsageLog>> GetByUserIdAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ApiUsageLog>> GetByProviderIdAsync(Guid providerId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ApiUsageLog>> GetByEndpointAsync(string endpoint, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<ApiUsageStatsResponse> GetUsageStatsAsync(Guid? userId = null, Guid? providerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<int> GetRequestCountAsync(Guid? userId = null, Guid? providerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ApiUsageLog>> GetByAccountTypeAsync(AccountType accountType, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetEndpointUsageAsync(Guid? userId = null, Guid? providerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task LogUsageAsync(ApiUsageLog usageLog, CancellationToken cancellationToken = default);
}