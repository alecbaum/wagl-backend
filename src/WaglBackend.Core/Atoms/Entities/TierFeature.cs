using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Atoms.Entities;

public class TierFeature
{
    public Guid Id { get; set; }
    public AccountTier RequiredTier { get; set; }
    public string FeatureName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public FeatureFlags FeatureFlag { get; set; }
    public int? RateLimitPerHour { get; set; }
    public int? DailyLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}