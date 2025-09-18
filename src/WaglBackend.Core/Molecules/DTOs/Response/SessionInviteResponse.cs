namespace WaglBackend.Core.Molecules.DTOs.Response;

public class SessionInviteResponse
{
    public string Id { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? InviteeEmail { get; set; }
    public string? InviteeName { get; set; }
    public bool IsConsumed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? ConsumedByUserId { get; set; }
    public string? ConsumedByName { get; set; }
    public bool IsExpired { get; set; }
    public bool IsValid { get; set; }
    public bool CanBeUsed { get; set; }
    public string InviteUrl { get; set; } = string.Empty;
}

public class BulkSessionInviteResponse
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalInvites { get; set; }
    public int SuccessfulInvites { get; set; }
    public int FailedInvites { get; set; }
    public List<SessionInviteResponse> Invites { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class InviteValidationResponse
{
    public bool IsValid { get; set; }
    public string? SessionId { get; set; }
    public string? SessionName { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public bool CanJoin { get; set; }
    public string? ErrorMessage { get; set; }
}

public class InviteStatisticsResponse
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalInvites { get; set; }
    public int ActiveInvites { get; set; }
    public int ConsumedInvites { get; set; }
    public int ExpiredInvites { get; set; }
    public double ConversionRate { get; set; }
    public DateTime? LastInviteCreated { get; set; }
    public DateTime? LastInviteConsumed { get; set; }
}