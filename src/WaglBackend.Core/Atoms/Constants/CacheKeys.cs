namespace WaglBackend.Core.Atoms.Constants;

public static class CacheKeys
{
    private const string Separator = ":";
    
    public const string ApiKeyPrefix = "apikey";
    public const string UserPrefix = "user";
    public const string ProviderPrefix = "provider";
    public const string RateLimitPrefix = "ratelimit";
    public const string TierFeaturesPrefix = "tierfeatures";
    public const string UserProfilePrefix = "userprofile";
    
    public static string ApiKey(string keyHash) => $"{ApiKeyPrefix}{Separator}{keyHash}";
    public static string User(Guid userId) => $"{UserPrefix}{Separator}{userId}";
    public static string Provider(Guid providerId) => $"{ProviderPrefix}{Separator}{providerId}";
    public static string UserProfile(Guid userId) => $"{UserProfilePrefix}{Separator}{userId}";
    public static string RateLimit(string identifier, string endpoint) => $"{RateLimitPrefix}{Separator}{identifier}{Separator}{endpoint}";
    public static string TierFeatures(string tier) => $"{TierFeaturesPrefix}{Separator}{tier}";
    
    public static class Patterns
    {
        public const string AllApiKeys = $"{ApiKeyPrefix}{Separator}*";
        public const string AllUsers = $"{UserPrefix}{Separator}*";
        public const string AllProviders = $"{ProviderPrefix}{Separator}*";
        public const string AllRateLimits = $"{RateLimitPrefix}{Separator}*";
        public const string AllTierFeatures = $"{TierFeaturesPrefix}{Separator}*";
        public const string AllUserProfiles = $"{UserProfilePrefix}{Separator}*";
    }
}