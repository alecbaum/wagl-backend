namespace WaglBackend.Core.Molecules.DTOs.Request.UAI;

/// <summary>
/// DTO for UAI health check /api/health endpoint
/// </summary>
public class UAIHealthCheckRequest
{
    public string Message { get; set; } = "Hello everyone!";
    public int UserID { get; set; } = 0;
    public int Room { get; set; } = 0;
}