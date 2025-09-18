using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.Exceptions;

public class ChatSessionStateException : BusinessRuleException
{
    public SessionId SessionId { get; }
    public SessionStatus CurrentStatus { get; }
    public SessionStatus? RequiredStatus { get; }

    public ChatSessionStateException(SessionId sessionId, SessionStatus currentStatus, string message)
        : base("SessionStateError", message)
    {
        SessionId = sessionId;
        CurrentStatus = currentStatus;
    }

    public ChatSessionStateException(SessionId sessionId, SessionStatus currentStatus, SessionStatus requiredStatus, string message)
        : base("SessionStateError", message)
    {
        SessionId = sessionId;
        CurrentStatus = currentStatus;
        RequiredStatus = requiredStatus;
    }

    public ChatSessionStateException(SessionId sessionId, SessionStatus currentStatus, string message, Exception innerException)
        : base("SessionStateError", message, innerException)
    {
        SessionId = sessionId;
        CurrentStatus = currentStatus;
    }
}

public class SessionNotActiveException : ChatSessionStateException
{
    public SessionNotActiveException(SessionId sessionId, SessionStatus currentStatus)
        : base(sessionId, currentStatus, SessionStatus.Active,
               $"Session '{sessionId}' is not active (current status: {currentStatus}). Only active sessions can be used for chat operations.")
    {
    }
}

public class SessionAlreadyEndedException : ChatSessionStateException
{
    public SessionAlreadyEndedException(SessionId sessionId)
        : base(sessionId, SessionStatus.Ended,
               $"Session '{sessionId}' has already ended and cannot be modified.")
    {
    }
}

public class SessionNotScheduledException : ChatSessionStateException
{
    public SessionNotScheduledException(SessionId sessionId, SessionStatus currentStatus)
        : base(sessionId, currentStatus, SessionStatus.Scheduled,
               $"Session '{sessionId}' is not scheduled (current status: {currentStatus}). Only scheduled sessions can be started.")
    {
    }
}

public class SessionCapacityExceededException : ChatSessionStateException
{
    public int CurrentParticipants { get; }
    public int MaxParticipants { get; }

    public SessionCapacityExceededException(SessionId sessionId, int currentParticipants, int maxParticipants)
        : base(sessionId, SessionStatus.Active,
               $"Session '{sessionId}' is at maximum capacity ({currentParticipants}/{maxParticipants}). Cannot add more participants.")
    {
        CurrentParticipants = currentParticipants;
        MaxParticipants = maxParticipants;
    }
}