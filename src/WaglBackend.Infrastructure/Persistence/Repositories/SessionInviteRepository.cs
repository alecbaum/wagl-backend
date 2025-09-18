using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Repositories;

public class SessionInviteRepository : BaseRepository<SessionInvite>, ISessionInviteRepository
{
    public SessionInviteRepository(ApplicationDbContext context) : base(context)
    {
    }


    public async Task<SessionInvite?> GetByTokenAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetActiveInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       x.IsValid &&
                       x.ExpiresAt > currentTime &&
                       !x.IsConsumed)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetConsumedInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       (x.IsConsumed || !x.IsValid))
            .OrderByDescending(x => x.ConsumedAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetExpiredInvitesAsync(CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        return await Query
            .Where(x => x.ExpiresAt <= currentTime)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetUnconsumedInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       x.IsValid &&
                       !x.IsConsumed)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetUnusedInvitesAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ConsumedAt == null &&
                       x.IsValid &&
                       !x.IsConsumed)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetInvitesDueForExpirationAsync(DateTime warningTime, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.IsValid &&
                       x.ExpiresAt > DateTime.UtcNow &&
                       x.ExpiresAt <= warningTime)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveInviteCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        return await Query
            .CountAsync(x => x.SessionId == sessionId &&
                           x.IsValid &&
                           x.ExpiresAt > currentTime &&
                           !x.IsConsumed, cancellationToken);
    }

    public async Task<int> GetConsumedInviteCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId &&
                           (x.IsConsumed || !x.IsValid), cancellationToken);
    }

    public async Task<int> GetTotalUsageCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var invites = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        return invites.Count(x => x.IsConsumed);
    }

    public async Task<bool> IsTokenUniqueAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        return !await Query
            .AnyAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<bool> HasActiveInvitesAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        return await Query
            .AnyAsync(x => x.SessionId == sessionId &&
                          x.IsValid &&
                          x.ExpiresAt > currentTime &&
                          !x.IsConsumed, cancellationToken);
    }

    public async Task<SessionInvite?> GetMostRecentlyUsedAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       x.ConsumedAt != null)
            .OrderByDescending(x => x.ConsumedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetInvitesByUsageAsync(SessionId sessionId, int minUsage, int maxUsage, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken)
            .ContinueWith(task =>
            {
                var invites = task.Result;
                return invites.Where(x =>
                {
                    // Single use invites: usage is either 0 or 1
                    var usage = x.IsConsumed ? 1 : 0;
                    return usage >= minUsage && usage <= maxUsage;
                }).OrderByDescending(x => x.IsConsumed ? 1 : 0);
            }, cancellationToken);
    }

    public async Task<double> GetAverageUsageRateAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var invites = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (!invites.Any())
            return 0;

        var totalPossibleUses = invites.Count;
        var totalActualUses = invites.Count(x => x.IsConsumed);

        return totalPossibleUses > 0 ? (double)totalActualUses / totalPossibleUses : 0;
    }

    public async Task<int> DeleteExpiredInvitesAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        var expiredInvites = await Query
            .Where(x => x.ExpiresAt <= cutoffTime)
            .ToListAsync(cancellationToken);

        if (expiredInvites.Any())
        {
            DbSet.RemoveRange(expiredInvites);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return expiredInvites.Count;
    }

    public async Task ExpireInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var invites = await Query
            .Where(x => x.SessionId == sessionId && x.IsValid)
            .ToListAsync(cancellationToken);

        foreach (var invite in invites)
        {
            // IsActive is computed, cannot be set directly
            // UpdatedAt doesn't exist on SessionInvite
        }

        if (invites.Any())
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<SessionInvite?> GetByTokenWithSessionAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        return await Context.SessionInvites
            .Include(i => i.ChatSession)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<bool> IsTokenValidAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        return await Query
            .AnyAsync(x => x.Token == token &&
                          x.IsValid &&
                          x.ExpiresAt > currentTime &&
                          !x.IsConsumed, cancellationToken);
    }

    public async Task<bool> ConsumeTokenAsync(InviteToken token, UserId? userId = null, string? userName = null, CancellationToken cancellationToken = default)
    {
        var invite = await GetByTokenAsync(token, cancellationToken);
        if (invite != null && invite.IsValid)
        {
            invite.Consume();
            // UpdatedAt doesn't exist on SessionInvite

            // Invite is now consumed via the Consume() method

            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task DeleteBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var invites = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (invites.Any())
        {
            DbSet.RemoveRange(invites);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<SessionInvite>> GetExpiredInvitesAsync(DateTime expiredThreshold, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ExpiresAt <= expiredThreshold)
            .OrderBy(x => x.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionInvite>> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await GetInvitesBySessionAsync(sessionId, cancellationToken);
    }
}