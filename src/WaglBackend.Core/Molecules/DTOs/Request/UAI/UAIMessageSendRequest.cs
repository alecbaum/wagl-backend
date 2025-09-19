namespace WaglBackend.Core.Molecules.DTOs.Request.UAI;

/// <summary>
/// DTO for sending messages to UAI service /api/message/send endpoint
/// </summary>
public class UAIMessageSendRequest
{
    public string Message { get; set; } = string.Empty;
    public long UserID { get; set; }  // Maps to our Participant.Id (as number)
    public int Room { get; set; }     // Maps to our room number (0, 1, or 2 in test env)
}