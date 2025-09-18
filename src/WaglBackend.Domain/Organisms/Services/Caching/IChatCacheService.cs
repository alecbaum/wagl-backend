using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services.Caching;

public interface IChatCacheService
{
    // Session Caching
    Task<ChatSessionResponse?> GetSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task SetSessionAsync(SessionId sessionId, ChatSessionResponse session, CancellationToken cancellationToken = default);
    Task RemoveSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);

    // Room Caching
    Task<ChatRoomResponse?> GetRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task SetRoomAsync(RoomId roomId, ChatRoomResponse room, CancellationToken cancellationToken = default);
    Task RemoveRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);

    // Participant Caching
    Task<List<ParticipantResponse>?> GetRoomParticipantsAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task SetRoomParticipantsAsync(RoomId roomId, List<ParticipantResponse> participants, CancellationToken cancellationToken = default);
    Task RemoveRoomParticipantsAsync(RoomId roomId, CancellationToken cancellationToken = default);

    // Connection Tracking
    Task<string?> GetConnectionRoomAsync(string connectionId, CancellationToken cancellationToken = default);
    Task SetConnectionRoomAsync(string connectionId, RoomId roomId, CancellationToken cancellationToken = default);
    Task RemoveConnectionRoomAsync(string connectionId, CancellationToken cancellationToken = default);

    // Invite Token Caching
    Task<SessionInviteResponse?> GetInviteAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task SetInviteAsync(InviteToken token, SessionInviteResponse invite, CancellationToken cancellationToken = default);
    Task RemoveInviteAsync(InviteToken token, CancellationToken cancellationToken = default);

    // Bulk Operations
    Task InvalidateSessionCacheAsync(SessionId sessionId, CancellationToken cancellationToken = default);
}