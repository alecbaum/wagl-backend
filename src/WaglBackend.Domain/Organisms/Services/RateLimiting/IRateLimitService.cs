using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services.RateLimiting;

public interface IRateLimitService
{
    Task<RateLimitResult> CheckRateLimitAsync(string identifier, string accountType, string endpoint, CancellationToken cancellationToken = default);
    Task<RateLimitInfoResponse> GetRateLimitInfoAsync(string identifier, string accountType, CancellationToken cancellationToken = default);
    Task<bool> ResetRateLimitAsync(string identifier, string accountType, CancellationToken cancellationToken = default);
    Task<Dictionary<string, RateLimitInfoResponse>> GetAllRateLimitsAsync(string identifier, CancellationToken cancellationToken = default);
}

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int Limit { get; set; }
    public int Used { get; set; }
    public int Remaining => Math.Max(0, Limit - Used);
    public DateTime ResetTime { get; set; }
    public TimeSpan RetryAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
}