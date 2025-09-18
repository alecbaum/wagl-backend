namespace WaglBackend.Core.Atoms.ValueObjects;

public class RoomId : IEquatable<RoomId>
{
    public Guid Value { get; private set; }

    private RoomId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RoomId cannot be empty.", nameof(value));

        Value = value;
    }

    public static RoomId Create() => new(Guid.NewGuid());

    public static RoomId From(Guid value) => new(value);

    public static RoomId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid GUID format: {value}", nameof(value));

        return new(guid);
    }

    public bool Equals(RoomId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as RoomId);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(RoomId? left, RoomId? right) => Equals(left, right);

    public static bool operator !=(RoomId? left, RoomId? right) => !Equals(left, right);

    public static implicit operator Guid(RoomId roomId) => roomId.Value;

    public static implicit operator string(RoomId roomId) => roomId.Value.ToString();

    public override string ToString() => Value.ToString();
}