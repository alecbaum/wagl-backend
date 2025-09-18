using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.DTOs.Response;

public class ParticipantResponse
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ConnectionId { get; set; }
    public ParticipantType Type { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsRegisteredUser { get; set; }
    public bool IsGuest { get; set; }
    public bool IsConnected { get; set; }
    public TimeSpan? Duration { get; set; }
    public int MessageCount { get; set; }
}

public class RoomJoinResponse
{
    public bool Success { get; set; }
    public string? RoomId { get; set; }
    public string? ParticipantId { get; set; }
    public string? ErrorMessage { get; set; }
    public ParticipantResponse? Participant { get; set; }
    public ChatRoomResponse? Room { get; set; }
}