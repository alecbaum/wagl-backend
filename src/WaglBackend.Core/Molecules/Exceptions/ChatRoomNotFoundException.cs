using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Molecules.Exceptions;

public class ChatRoomNotFoundException : BusinessRuleException
{
    public RoomId RoomId { get; }

    public ChatRoomNotFoundException(RoomId roomId)
        : base("RoomNotFound", $"Chat room with ID '{roomId}' was not found.")
    {
        RoomId = roomId;
    }

    public ChatRoomNotFoundException(RoomId roomId, string message)
        : base("RoomNotFound", message)
    {
        RoomId = roomId;
    }

    public ChatRoomNotFoundException(RoomId roomId, string message, Exception innerException)
        : base("RoomNotFound", message, innerException)
    {
        RoomId = roomId;
    }
}