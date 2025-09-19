using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

public interface IChatMessageService
{
    Task<ChatMessageResponse> SendMessageAsync(ChatMessageRequest request, Guid participantId, CancellationToken cancellationToken = default);
    Task<ChatMessageResponse?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<MessageHistoryResponse> GetMessageHistoryAsync(RoomId roomId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetRecentMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetMessagesAfterAsync(RoomId roomId, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetMessagesBeforeAsync(RoomId roomId, DateTime timestamp, int count = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetMessagesByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> DeleteMessageAsync(Guid messageId, Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> CanDeleteMessageAsync(Guid messageId, Guid participantId, CancellationToken cancellationToken = default);
    Task<int> GetMessageCountByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> GetMessageCountByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> DeleteMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<bool> IsMessageValidAsync(string content, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetRoomMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> GetRoomMessageCountAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageResponse>> GetRecentRoomMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default);
    Task<double> GetAverageMessageLengthAsync(RoomId roomId, CancellationToken cancellationToken = default);

    // UAI Integration Methods for Inbound Messages
    // TODO: Placeholder methods - UAI doesn't send bot/moderator messages yet
    Task<ChatMessageResponse> CreateModeratorMessageAsync(SessionId sessionId, string content, string externalMessageId, Guid? triggerMessageId = null, CancellationToken cancellationToken = default);
    Task<ChatMessageResponse> CreateBotMessageAsync(RoomId roomId, SessionId sessionId, string content, string externalMessageId, Guid? triggerMessageId = null, string botName = "UAI Bot", CancellationToken cancellationToken = default);
    Task<bool> MessageExistsAsync(string externalMessageId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoomId>> GetSessionRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
}