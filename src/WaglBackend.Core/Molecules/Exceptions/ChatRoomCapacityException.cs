using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Core.Molecules.Exceptions;

public class ChatRoomCapacityException : BusinessRuleException
{
    public RoomId RoomId { get; }
    public int CurrentCapacity { get; }
    public int MaximumCapacity { get; }

    public ChatRoomCapacityException(RoomId roomId, int currentCapacity, int maximumCapacity)
        : base($"Chat room '{roomId}' is at capacity ({currentCapacity}/{maximumCapacity}). Cannot add more participants.")
    {
        RoomId = roomId;
        CurrentCapacity = currentCapacity;
        MaximumCapacity = maximumCapacity;
    }

    public ChatRoomCapacityException(RoomId roomId, int currentCapacity, int maximumCapacity, string message)
        : base(message)
    {
        RoomId = roomId;
        CurrentCapacity = currentCapacity;
        MaximumCapacity = maximumCapacity;
    }
}

public class NoAvailableRoomsException : BusinessRuleException
{
    public SessionId SessionId { get; }

    public NoAvailableRoomsException(SessionId sessionId)
        : base($"No available rooms found for session '{sessionId}'. All rooms are at capacity.")
    {
        SessionId = sessionId;
    }

    public NoAvailableRoomsException(SessionId sessionId, string message)
        : base(message)
    {
        SessionId = sessionId;
    }
}