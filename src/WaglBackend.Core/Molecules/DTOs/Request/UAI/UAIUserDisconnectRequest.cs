namespace WaglBackend.Core.Molecules.DTOs.Request.UAI;

/// <summary>
/// DTO for disconnecting users from UAI service /api/user/disconnect endpoint
/// </summary>
public class UAIUserDisconnectRequest
{
    public string Username { get; set; } = string.Empty;  // Participant DisplayName
    public long UniqueID { get; set; }                    // Maps to our Participant.Id (as number)
    public string UrlParams { get; set; } = "?source=web&version=1.0";  // Default params
    public int Room { get; set; }                         // Room number (0, 1, or 2 in test env)
}