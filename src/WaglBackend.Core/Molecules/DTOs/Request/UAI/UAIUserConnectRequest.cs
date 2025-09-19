namespace WaglBackend.Core.Molecules.DTOs.Request.UAI;

/// <summary>
/// DTO for connecting users to UAI service /api/user/connect endpoint
/// </summary>
public class UAIUserConnectRequest
{
    public string Username { get; set; } = string.Empty;  // Participant DisplayName
    public long UniqueID { get; set; }                    // Maps to our Participant.Id (as number)
    public string UrlParams { get; set; } = "?source=web&version=1.0";  // Default params
    public int Room { get; set; }                         // Room number (0, 1, or 2 in test env)
}