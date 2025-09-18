using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Molecules.Exceptions;

public class ChatSessionNotFoundException : BusinessRuleException
{
    public SessionId SessionId { get; }

    public ChatSessionNotFoundException(SessionId sessionId)
        : base("SessionNotFound", $"Chat session with ID '{sessionId}' was not found.")
    {
        SessionId = sessionId;
    }

    public ChatSessionNotFoundException(SessionId sessionId, string message)
        : base("SessionNotFound", message)
    {
        SessionId = sessionId;
    }

    public ChatSessionNotFoundException(SessionId sessionId, string message, Exception innerException)
        : base("SessionNotFound", message, innerException)
    {
        SessionId = sessionId;
    }
}