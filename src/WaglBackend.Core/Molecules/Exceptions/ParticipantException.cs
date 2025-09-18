using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Molecules.Exceptions;

public class ParticipantException : BusinessRuleException
{
    public UserId? UserId { get; }
    public string? ConnectionId { get; }

    public ParticipantException(string message)
        : base("ParticipantError", message)
    {
    }

    public ParticipantException(UserId userId, string message)
        : base("ParticipantError", message)
    {
        UserId = userId;
    }

    public ParticipantException(string connectionId, string message)
        : base("ParticipantError", message)
    {
        ConnectionId = connectionId;
    }

    public ParticipantException(string message, Exception innerException)
        : base("ParticipantError", message, innerException)
    {
    }
}

public class ParticipantAlreadyInSessionException : ParticipantException
{
    public SessionId SessionId { get; }

    public ParticipantAlreadyInSessionException(UserId userId, SessionId sessionId)
        : base(userId, $"User '{userId}' is already a participant in session '{sessionId}'.")
    {
        SessionId = sessionId;
    }
}

public class ParticipantNotInSessionException : ParticipantException
{
    public SessionId SessionId { get; }

    public ParticipantNotInSessionException(UserId userId, SessionId sessionId)
        : base(userId, $"User '{userId}' is not a participant in session '{sessionId}'.")
    {
        SessionId = sessionId;
    }
}

public class ParticipantNotInRoomException : ParticipantException
{
    public RoomId RoomId { get; }

    public ParticipantNotInRoomException(UserId userId, RoomId roomId)
        : base(userId, $"User '{userId}' is not a participant in room '{roomId}'.")
    {
        RoomId = roomId;
    }
}

public class InvalidConnectionException : ParticipantException
{
    public InvalidConnectionException(string connectionId)
        : base(connectionId, $"Invalid or expired connection: {connectionId}")
    {
    }

    public InvalidConnectionException(string connectionId, string message)
        : base(connectionId, message)
    {
    }
}