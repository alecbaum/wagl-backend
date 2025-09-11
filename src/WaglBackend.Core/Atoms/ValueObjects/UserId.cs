namespace WaglBackend.Core.Atoms.ValueObjects;

public class UserId : IEquatable<UserId>
{
    public Guid Value { get; private set; }

    private UserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(value));

        Value = value;
    }

    public static UserId Create() => new(Guid.NewGuid());

    public static UserId From(Guid value) => new(value);

    public static UserId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid GUID format: {value}", nameof(value));

        return new(guid);
    }

    public bool Equals(UserId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as UserId);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(UserId? left, UserId? right) => Equals(left, right);

    public static bool operator !=(UserId? left, UserId? right) => !Equals(left, right);

    public static implicit operator Guid(UserId userId) => userId.Value;

    public static implicit operator string(UserId userId) => userId.Value.ToString();

    public override string ToString() => Value.ToString();
}