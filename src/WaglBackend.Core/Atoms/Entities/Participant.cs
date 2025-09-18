using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Atoms.Entities;

public class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RoomId RoomId { get; set; } = null!;
    public SessionId SessionId { get; set; } = null!;
    public UserId? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ConnectionId { get; set; }
    public ParticipantType Type { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ChatRoom ChatRoom { get; set; } = null!;
    public ChatSession ChatSession { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public bool IsRegisteredUser => Type == ParticipantType.RegisteredUser && UserId != null;
    public bool IsGuest => Type == ParticipantType.GuestUser;
    public bool IsConnected => !string.IsNullOrEmpty(ConnectionId);
    public TimeSpan? Duration => LeftAt?.Subtract(JoinedAt);

    public void Leave()
    {
        IsActive = false;
        LeftAt = DateTime.UtcNow;
        ConnectionId = null;
    }

    public void UpdateConnection(string connectionId)
    {
        ConnectionId = connectionId;
        IsActive = true;
    }
}