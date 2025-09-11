namespace WaglBackend.Core.Atoms.Constants;

public static class ErrorCodes
{
    public const string GeneralError = "GENERAL_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFoundError = "NOT_FOUND_ERROR";
    public const string UnauthorizedError = "UNAUTHORIZED_ERROR";
    public const string ForbiddenError = "FORBIDDEN_ERROR";
    public const string ConflictError = "CONFLICT_ERROR";
    public const string RateLimitError = "RATE_LIMIT_ERROR";
    
    public static class Authentication
    {
        public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
        public const string InvalidApiKey = "AUTH_INVALID_API_KEY";
        public const string ExpiredToken = "AUTH_EXPIRED_TOKEN";
        public const string InvalidToken = "AUTH_INVALID_TOKEN";
        public const string MissingToken = "AUTH_MISSING_TOKEN";
        public const string AccountDisabled = "AUTH_ACCOUNT_DISABLED";
        public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
    }
    
    public static class Authorization
    {
        public const string InsufficientPermissions = "AUTHZ_INSUFFICIENT_PERMISSIONS";
        public const string TierLimitExceeded = "AUTHZ_TIER_LIMIT_EXCEEDED";
        public const string FeatureNotAvailable = "AUTHZ_FEATURE_NOT_AVAILABLE";
        public const string AccessDenied = "AUTHZ_ACCESS_DENIED";
    }
    
    public static class RateLimit
    {
        public const string LimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string QuotaExceeded = "QUOTA_EXCEEDED";
        public const string TierLimitReached = "TIER_LIMIT_REACHED";
    }
    
    public static class User
    {
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string UserAlreadyExists = "USER_ALREADY_EXISTS";
        public const string InvalidUserData = "USER_INVALID_DATA";
        public const string EmailAlreadyInUse = "USER_EMAIL_ALREADY_IN_USE";
        public const string WeakPassword = "USER_WEAK_PASSWORD";
    }
    
    public static class Provider
    {
        public const string ProviderNotFound = "PROVIDER_NOT_FOUND";
        public const string ProviderAlreadyExists = "PROVIDER_ALREADY_EXISTS";
        public const string InvalidProviderData = "PROVIDER_INVALID_DATA";
        public const string ApiKeyGenerationFailed = "PROVIDER_API_KEY_GENERATION_FAILED";
        public const string ProviderDisabled = "PROVIDER_DISABLED";
    }
    
    public static class Cache
    {
        public const string CacheUnavailable = "CACHE_UNAVAILABLE";
        public const string CacheKeyNotFound = "CACHE_KEY_NOT_FOUND";
        public const string CacheOperationFailed = "CACHE_OPERATION_FAILED";
    }
    
    public static class Database
    {
        public const string DatabaseUnavailable = "DB_UNAVAILABLE";
        public const string DatabaseTimeout = "DB_TIMEOUT";
        public const string DatabaseConstraintViolation = "DB_CONSTRAINT_VIOLATION";
        public const string DatabaseOperationFailed = "DB_OPERATION_FAILED";
    }
}