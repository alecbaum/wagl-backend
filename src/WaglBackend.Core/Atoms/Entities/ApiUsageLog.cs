using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Atoms.Entities;

public class ApiUsageLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ProviderId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public AccountType AccountType { get; set; }
    public int RequestSize { get; set; }
    public int ResponseSize { get; set; }
    
    public User? User { get; set; }
    public Provider? Provider { get; set; }
}