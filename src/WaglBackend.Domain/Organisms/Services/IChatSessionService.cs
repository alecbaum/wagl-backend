using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

public interface IChatSessionService
{
    Task<ChatSessionResponse> CreateSessionAsync(ChatSessionRequest request, UserId? createdByUserId = null, CancellationToken cancellationToken = default);
    Task<ChatSessionResponse?> GetSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<ChatSessionResponse?> GetSessionWithDetailsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSessionSummaryResponse>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSessionSummaryResponse>> GetScheduledSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatSessionSummaryResponse>> GetSessionsByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<bool> StartSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> EndSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> CancelSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> CanStartSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> IsSessionActiveAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> UpdateSessionStatusAsync(SessionId sessionId, SessionStatus status, CancellationToken cancellationToken = default);
    Task<int> GetActiveParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
}