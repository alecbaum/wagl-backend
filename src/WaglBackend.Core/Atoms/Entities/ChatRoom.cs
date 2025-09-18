using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Atoms.Entities;

public class ChatRoom
{
    public RoomId Id { get; set; } = RoomId.Create();
    public SessionId SessionId { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int ParticipantCount { get; set; } = 0;
    public int MaxParticipants { get; set; } = 6;
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public ChatSession ChatSession { get; set; } = null!;
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public bool IsFull => ParticipantCount >= MaxParticipants;
    public bool HasSpace => ParticipantCount < MaxParticipants;
    public bool IsActive => Status == RoomStatus.Active;
    public int AvailableSlots => MaxParticipants - ParticipantCount;
}