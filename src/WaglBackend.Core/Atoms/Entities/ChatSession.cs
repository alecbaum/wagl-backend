using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Atoms.Entities;

public class ChatSession
{
    public SessionId Id { get; set; } = SessionId.Create();
    public string Name { get; set; } = string.Empty;
    public DateTime ScheduledStartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int MaxParticipants { get; set; } = 36; // 6 rooms * 6 participants each
    public int MaxParticipantsPerRoom { get; set; } = 6;
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public UserId? CreatedByUserId { get; set; }

    public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
    public ICollection<SessionInvite> SessionInvites { get; set; } = new List<SessionInvite>();

    public DateTime ScheduledEndTime => ScheduledStartTime.Add(Duration);
    public bool IsExpired => DateTime.UtcNow > ScheduledEndTime;
    public bool CanStart => Status == SessionStatus.Scheduled && DateTime.UtcNow >= ScheduledStartTime;
    public bool IsActive => Status == SessionStatus.Active;
}