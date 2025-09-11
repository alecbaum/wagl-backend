using WaglBackend.Core.Atoms.Constants;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.Exceptions;

public class TierLimitExceededException : Exception
{
    public string ErrorCode { get; }
    public AccountTier CurrentTier { get; }
    public AccountTier RequiredTier { get; }

    public TierLimitExceededException(AccountTier currentTier, AccountTier requiredTier) 
        : base($"Current tier ({currentTier}) does not have access to this feature. Required tier: {requiredTier}")
    {
        ErrorCode = ErrorCodes.Authorization.TierLimitExceeded;
        CurrentTier = currentTier;
        RequiredTier = requiredTier;
    }

    public TierLimitExceededException(AccountTier currentTier, AccountTier requiredTier, string message) 
        : base(message)
    {
        ErrorCode = ErrorCodes.Authorization.TierLimitExceeded;
        CurrentTier = currentTier;
        RequiredTier = requiredTier;
    }

    public TierLimitExceededException(AccountTier currentTier, AccountTier requiredTier, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = ErrorCodes.Authorization.TierLimitExceeded;
        CurrentTier = currentTier;
        RequiredTier = requiredTier;
    }
}