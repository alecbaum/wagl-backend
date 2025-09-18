using System.ComponentModel.DataAnnotations;

namespace WaglBackend.Core.Molecules.DTOs.Request;

public class ChatSessionRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduledStartTime { get; set; }

    [Required]
    [Range(1, 1440)] // 1 minute to 24 hours
    public int DurationMinutes { get; set; }

    [Range(6, 36)] // Minimum 1 room (6 people), maximum 6 rooms (36 people)
    public int MaxParticipants { get; set; } = 36;

    [Range(2, 6)]
    public int MaxParticipantsPerRoom { get; set; } = 6;
}