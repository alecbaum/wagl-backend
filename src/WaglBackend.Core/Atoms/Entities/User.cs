using Microsoft.AspNetCore.Identity;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Atoms.Entities;

public class User : IdentityUser<Guid>
{
    public TierLevel TierLevel { get; set; } = TierLevel.FromTier(AccountTier.Tier1);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    
    public ICollection<ApiUsageLog> ApiUsageLogs { get; set; } = new List<ApiUsageLog>();
}