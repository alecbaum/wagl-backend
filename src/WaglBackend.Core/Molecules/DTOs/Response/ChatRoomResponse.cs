using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.DTOs.Response;

public class ChatRoomResponse
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public RoomStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsFull { get; set; }
    public bool HasSpace { get; set; }
    public bool IsActive { get; set; }
    public int AvailableSlots { get; set; }
    public List<ParticipantResponse> Participants { get; set; } = new();
    public List<ChatMessageResponse> RecentMessages { get; set; } = new();
}

public class ChatRoomSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public RoomStatus Status { get; set; }
    public bool HasSpace { get; set; }
    public int AvailableSlots { get; set; }
}