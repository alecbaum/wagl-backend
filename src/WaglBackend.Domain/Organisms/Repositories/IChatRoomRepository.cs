using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IChatRoomRepository : IRepository<ChatRoom>
{
    Task<ChatRoom?> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<ChatRoom?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<ChatRoom?> GetWithParticipantsAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<ChatRoom?> GetWithMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<ChatRoom?> GetWithParticipantsAndMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetActiveRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetAvailableRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetRoomsByStatusAsync(RoomStatus status, CancellationToken cancellationToken = default);
    Task<ChatRoom?> GetLeastPopulatedRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> UpdateParticipantCountAsync(RoomId roomId, int count, CancellationToken cancellationToken = default);
    Task<int> GetTotalParticipantsInSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetEmptyRoomsAsync(CancellationToken cancellationToken = default);
}