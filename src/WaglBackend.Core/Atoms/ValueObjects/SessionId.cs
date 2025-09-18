namespace WaglBackend.Core.Atoms.ValueObjects;

public class SessionId : IEquatable<SessionId>
{
    public Guid Value { get; private set; }

    private SessionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty.", nameof(value));

        Value = value;
    }

    public static SessionId Create() => new(Guid.NewGuid());

    public static SessionId From(Guid value) => new(value);

    public static SessionId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"Invalid GUID format: {value}", nameof(value));

        return new(guid);
    }

    public bool Equals(SessionId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as SessionId);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(SessionId? left, SessionId? right) => Equals(left, right);

    public static bool operator !=(SessionId? left, SessionId? right) => !Equals(left, right);

    public static implicit operator Guid(SessionId sessionId) => sessionId.Value;

    public static implicit operator string(SessionId sessionId) => sessionId.Value.ToString();

    public override string ToString() => Value.ToString();
}