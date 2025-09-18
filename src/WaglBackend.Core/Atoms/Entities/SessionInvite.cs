using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Atoms.Entities;

public class SessionInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public InviteToken Token { get; set; } = InviteToken.Create();
    public SessionId SessionId { get; set; } = null!;
    public string? InviteeEmail { get; set; }
    public string? InviteeName { get; set; }
    public bool IsConsumed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public UserId? ConsumedByUserId { get; set; }
    public string? ConsumedByName { get; set; }
    public DateTime? ExpiredAt { get; set; }

    public ChatSession ChatSession { get; set; } = null!;

    public bool IsExpired { get; set; }
    public bool IsExpiredByTime => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => !IsConsumed && !IsExpired && !IsExpiredByTime;
    public bool CanBeUsed => IsValid && ChatSession.CanStart;

    public void Consume(UserId? userId = null, string? userName = null)
    {
        if (IsConsumed)
            throw new InvalidOperationException("Invite has already been consumed.");

        if (IsExpired || IsExpiredByTime)
            throw new InvalidOperationException("Invite has expired.");

        IsConsumed = true;
        ConsumedAt = DateTime.UtcNow;
        ConsumedByUserId = userId;
        ConsumedByName = userName;
    }
}