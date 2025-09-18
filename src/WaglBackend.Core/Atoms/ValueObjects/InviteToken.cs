using System.Security.Cryptography;

namespace WaglBackend.Core.Atoms.ValueObjects;

public class InviteToken : IEquatable<InviteToken>
{
    public string Value { get; private set; }

    private InviteToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("InviteToken cannot be empty.", nameof(value));

        if (value.Length < 32)
            throw new ArgumentException("InviteToken must be at least 32 characters long.", nameof(value));

        Value = value;
    }

    public static InviteToken Create()
    {
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        return new(token);
    }

    public static InviteToken From(string value) => new(value);

    public bool Equals(InviteToken? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.Equals(other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as InviteToken);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(InviteToken? left, InviteToken? right) => Equals(left, right);

    public static bool operator !=(InviteToken? left, InviteToken? right) => !Equals(left, right);

    public static implicit operator string(InviteToken token) => token.Value;

    public override string ToString() => Value;
}