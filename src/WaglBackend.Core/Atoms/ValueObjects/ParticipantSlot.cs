namespace WaglBackend.Core.Atoms.ValueObjects;

public class ParticipantSlot : IEquatable<ParticipantSlot>
{
    public int Position { get; private set; }
    public bool IsOccupied { get; private set; }
    public Guid? ParticipantId { get; private set; }

    private ParticipantSlot(int position, bool isOccupied = false, Guid? participantId = null)
    {
        if (position < 1 || position > 6)
            throw new ArgumentException("Position must be between 1 and 6.", nameof(position));

        if (isOccupied && participantId == null)
            throw new ArgumentException("ParticipantId must be provided when slot is occupied.", nameof(participantId));

        if (!isOccupied && participantId != null)
            throw new ArgumentException("ParticipantId must be null when slot is not occupied.", nameof(participantId));

        Position = position;
        IsOccupied = isOccupied;
        ParticipantId = participantId;
    }

    public static ParticipantSlot Empty(int position) => new(position);

    public static ParticipantSlot Occupied(int position, Guid participantId) => new(position, true, participantId);

    public ParticipantSlot Occupy(Guid participantId)
    {
        if (IsOccupied)
            throw new InvalidOperationException("Slot is already occupied.");

        return new(Position, true, participantId);
    }

    public ParticipantSlot Free()
    {
        if (!IsOccupied)
            throw new InvalidOperationException("Slot is already free.");

        return new(Position);
    }

    public bool Equals(ParticipantSlot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Position == other.Position && IsOccupied == other.IsOccupied && ParticipantId == other.ParticipantId;
    }

    public override bool Equals(object? obj) => Equals(obj as ParticipantSlot);

    public override int GetHashCode() => HashCode.Combine(Position, IsOccupied, ParticipantId);

    public static bool operator ==(ParticipantSlot? left, ParticipantSlot? right) => Equals(left, right);

    public static bool operator !=(ParticipantSlot? left, ParticipantSlot? right) => !Equals(left, right);

    public override string ToString() => IsOccupied ? $"Slot {Position}: Occupied by {ParticipantId}" : $"Slot {Position}: Empty";
}