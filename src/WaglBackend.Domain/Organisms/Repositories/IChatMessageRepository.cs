using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IChatMessageRepository : IRepository<ChatMessage>
{
    Task<IEnumerable<ChatMessage>> GetMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesByRoomPaginatedAsync(RoomId roomId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesAfterAsync(RoomId roomId, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesBeforeAsync(RoomId roomId, DateTime timestamp, int count = 50, CancellationToken cancellationToken = default);
    Task<int> GetMessageCountByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> GetMessageCountByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<int> DeleteMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> DeleteByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> ArchiveMessagesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default);

    // UAI Integration methods
    Task<ChatMessage?> GetByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default);
}