using System.ComponentModel.DataAnnotations;

namespace WaglBackend.Core.Molecules.DTOs.Request;

public class ChatMessageRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string RoomId { get; set; } = string.Empty;
}

public class JoinRoomRequest
{
    [Required]
    public string InviteToken { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;
}