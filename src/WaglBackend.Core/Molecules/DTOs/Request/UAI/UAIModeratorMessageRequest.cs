namespace WaglBackend.Core.Molecules.DTOs.Request.UAI;

/// <summary>
/// TODO: Placeholder - UAI doesn't send moderator messages yet
/// DTO for receiving moderator messages from UAI service (future webhook)
/// </summary>
public class UAIModeratorMessageRequest
{
    public string MessageId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;  // UAI session IP (e.g., "54.196.26.13")
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? TriggerMessageId { get; set; }  // What user message triggered this
}