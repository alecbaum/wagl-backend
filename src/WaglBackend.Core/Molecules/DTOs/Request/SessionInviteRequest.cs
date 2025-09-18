using System.ComponentModel.DataAnnotations;

namespace WaglBackend.Core.Molecules.DTOs.Request;

public class SessionInviteRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [EmailAddress]
    public string? InviteeEmail { get; set; }

    [StringLength(100)]
    public string? InviteeName { get; set; }

    [Range(1, 1440)] // 1 minute to 24 hours
    public int ExpirationMinutes { get; set; } = 60;
}

public class BulkSessionInviteRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(36)]
    public List<InviteRecipient> Recipients { get; set; } = new();

    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;
}

public class InviteRecipient
{
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }
}