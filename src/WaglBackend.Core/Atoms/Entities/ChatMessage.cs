using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Atoms.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RoomId RoomId { get; set; } = null!;
    public SessionId SessionId { get; set; } = null!;
    public Guid ParticipantId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // UAI Integration Properties
    public MessageType MessageType { get; set; } = MessageType.UserMessage;
    public string? ExternalMessageId { get; set; }  // UAI message ID for tracking
    public Guid? TriggerMessageId { get; set; }     // What message triggered this (for bot/moderator responses)

    public ChatRoom ChatRoom { get; set; } = null!;
    public ChatSession ChatSession { get; set; } = null!;
    public Participant Participant { get; set; } = null!;

    public bool IsValid => !IsDeleted && !string.IsNullOrWhiteSpace(Content);
    public string SenderName => Participant?.DisplayName ?? "Unknown";

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}