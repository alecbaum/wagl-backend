using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.DTOs.Response;

public class ChatSessionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ScheduledStartTime { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxParticipants { get; set; }
    public int MaxParticipantsPerRoom { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime ScheduledEndTime { get; set; }
    public bool IsExpired { get; set; }
    public bool CanStart { get; set; }
    public bool IsActive { get; set; }
    public int TotalRooms { get; set; }
    public int ActiveParticipants { get; set; }
    public List<ChatRoomResponse> ChatRooms { get; set; } = new();
}

public class ChatSessionSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ScheduledStartTime { get; set; }
    public SessionStatus Status { get; set; }
    public int TotalRooms { get; set; }
    public int ActiveParticipants { get; set; }
    public bool CanStart { get; set; }
}