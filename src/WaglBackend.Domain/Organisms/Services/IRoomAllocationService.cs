using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

public interface IRoomAllocationService
{
    Task<RoomJoinResponse> AllocateParticipantToRoomAsync(SessionId sessionId, string displayName, UserId? userId = null, CancellationToken cancellationToken = default);
    Task<ChatRoom?> FindAvailableRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<ChatRoom> CreateNewRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> AddParticipantToRoomAsync(RoomId roomId, Participant participant, CancellationToken cancellationToken = default);
    Task<bool> RemoveParticipantFromRoomAsync(RoomId roomId, Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> IsRoomAvailableAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<bool> CanJoinRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> GetAvailableSlotsAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoomSummaryResponse>> GetAvailableRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> ConsolidateRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> UpdateRoomStatusAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<ChatRoomResponse?> GetRoomDetailsAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<ChatRoomResponse?> GetRoomWithDetailsAsync(RoomId roomId, CancellationToken cancellationToken = default);
}