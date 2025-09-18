using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<ChatSession?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetWithRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetWithRoomsAndParticipantsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetScheduledSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetExpiredSessionsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetCompletedSessionsBeforeAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetSessionsByStatusAsync(SessionStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSession>> GetSessionsByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSessionAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<int> GetActiveParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task ArchiveSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
}