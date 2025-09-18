using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Molecules.Exceptions;

public class SessionInviteException : BusinessRuleException
{
    public InviteToken? Token { get; }

    public SessionInviteException(string message)
        : base("InviteError", message)
    {
    }

    public SessionInviteException(InviteToken token, string message)
        : base("InviteError", message)
    {
        Token = token;
    }

    public SessionInviteException(string message, Exception innerException)
        : base("InviteError", message, innerException)
    {
    }

    public SessionInviteException(InviteToken token, string message, Exception innerException)
        : base("InviteError", message, innerException)
    {
        Token = token;
    }
}

public class InvalidInviteTokenException : SessionInviteException
{
    public InvalidInviteTokenException(InviteToken token)
        : base(token, $"Invalid or expired invite token: {token}")
    {
    }
}

public class InviteTokenAlreadyUsedException : SessionInviteException
{
    public InviteTokenAlreadyUsedException(InviteToken token)
        : base(token, $"Invite token has already been used: {token}")
    {
    }
}

public class InviteTokenExpiredException : SessionInviteException
{
    public DateTime ExpiredAt { get; }

    public InviteTokenExpiredException(InviteToken token, DateTime expiredAt)
        : base(token, $"Invite token expired at {expiredAt:yyyy-MM-dd HH:mm:ss} UTC: {token}")
    {
        ExpiredAt = expiredAt;
    }
}