namespace WaglBackend.Core.Molecules.Configurations;

public class RateLimitConfiguration
{
    public const string SectionName = "RateLimiting";
    
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableRedisRateLimiting { get; set; } = true;
    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new();
}

public class RateLimitPolicy
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int QueueLimit { get; set; } = 0;
    public TimeSpan QueueProcessingOrder { get; set; } = TimeSpan.Zero;
    public bool AutoReplenishment { get; set; } = true;
}