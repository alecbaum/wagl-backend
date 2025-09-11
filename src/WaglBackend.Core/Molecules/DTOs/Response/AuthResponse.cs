using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.DTOs.Response;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserProfileResponse User { get; set; } = new();
}

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public AccountTier TierLevel { get; set; }
    public string[] AvailableFeatures { get; set; } = Array.Empty<string>();
    public int HourlyRateLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}