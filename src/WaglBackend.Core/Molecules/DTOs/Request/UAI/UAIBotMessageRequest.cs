namespace WaglBackend.Core.Molecules.DTOs.Request.UAI;

/// <summary>
/// TODO: Placeholder - UAI doesn't send bot messages yet
/// DTO for receiving bot messages from UAI service (future webhook)
/// </summary>
public class UAIBotMessageRequest
{
    public string MessageId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;  // UAI session IP (e.g., "54.196.26.13")
    public int Room { get; set; }                          // Target room number (0, 1, or 2)
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? BotName { get; set; }                   // Optional bot identifier
    public string? TriggerMessageId { get; set; }          // What user message triggered this
}