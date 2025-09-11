using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Atoms.Entities;

public class Provider
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ApiKey? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public string? ContactEmail { get; set; }
    public string? Description { get; set; }
    public string[]? AllowedIpAddresses { get; set; }
    
    public ICollection<ApiUsageLog> ApiUsageLogs { get; set; } = new List<ApiUsageLog>();
}