using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.DTOs.Response;

public class ChatMessageResponse
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderUserId { get; set; }
    public bool IsFromCurrentUser { get; set; }

    // UAI Integration Properties
    public MessageType MessageType { get; set; } = MessageType.UserMessage;
    public string? ExternalMessageId { get; set; }  // UAI message ID
    public string? TriggerMessageId { get; set; }   // What message triggered this
}

public class MessageHistoryResponse
{
    public string RoomId { get; set; } = string.Empty;
    public int TotalMessages { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public List<ChatMessageResponse> Messages { get; set; } = new();
}