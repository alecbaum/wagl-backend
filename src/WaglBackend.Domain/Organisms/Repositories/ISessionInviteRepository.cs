using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface ISessionInviteRepository : IRepository<SessionInvite>
{
    Task<SessionInvite?> GetByTokenAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<SessionInvite?> GetByTokenWithSessionAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetActiveInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetExpiredInvitesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetConsumedInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetUnconsumedInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<bool> ConsumeTokenAsync(InviteToken token, UserId? userId = null, string? userName = null, CancellationToken cancellationToken = default);
    Task<int> GetActiveInviteCountAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetExpiredInvitesAsync(DateTime expiredThreshold, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInvite>> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
}