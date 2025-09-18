using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Molecules.Exceptions;

public class ChatMessageException : BusinessRuleException
{
    public UserId? SenderId { get; }
    public RoomId? RoomId { get; }

    public ChatMessageException(string message)
        : base("MessageError", message)
    {
    }

    public ChatMessageException(UserId senderId, string message)
        : base("MessageError", message)
    {
        SenderId = senderId;
    }

    public ChatMessageException(UserId senderId, RoomId roomId, string message)
        : base("MessageError", message)
    {
        SenderId = senderId;
        RoomId = roomId;
    }

    public ChatMessageException(string message, Exception innerException)
        : base("MessageError", message, innerException)
    {
    }
}

public class MessageContentTooLongException : ChatMessageException
{
    public int MaxLength { get; }
    public int ActualLength { get; }

    public MessageContentTooLongException(int maxLength, int actualLength)
        : base($"Message content is too long. Maximum length: {maxLength}, actual length: {actualLength}")
    {
        MaxLength = maxLength;
        ActualLength = actualLength;
    }
}

public class EmptyMessageException : ChatMessageException
{
    public EmptyMessageException()
        : base("Message content cannot be empty or whitespace only.")
    {
    }
}

public class MessageNotFoundException : ChatMessageException
{
    public Guid MessageId { get; }

    public MessageNotFoundException(Guid messageId)
        : base($"Chat message with ID '{messageId}' was not found.")
    {
        MessageId = messageId;
    }
}

public class UnauthorizedMessageException : ChatMessageException
{
    public UnauthorizedMessageException(UserId senderId, RoomId roomId)
        : base(senderId, roomId, $"User '{senderId}' is not authorized to send messages in room '{roomId}'.")
    {
    }
}