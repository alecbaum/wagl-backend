namespace WaglBackend.Core.Molecules.DTOs.Response;

public class ProviderResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public string[]? AllowedIpAddresses { get; set; }
    public string? ApiKeyPreview { get; set; }
    public int HourlyRateLimit { get; set; } = 10000;
}