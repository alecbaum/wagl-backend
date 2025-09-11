using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTierAsync(AccountTier tier, CancellationToken cancellationToken = default);
    Task<bool> UpdateLastLoginAsync(Guid userId, DateTime lastLoginAt, CancellationToken cancellationToken = default);
    Task<bool> UpdateTierAsync(Guid userId, AccountTier newTier, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersBySubscriptionExpiryAsync(DateTime expiryDate, CancellationToken cancellationToken = default);
}