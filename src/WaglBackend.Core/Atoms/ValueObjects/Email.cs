using System.Text.RegularExpressions;

namespace WaglBackend.Core.Atoms.ValueObjects;

public class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; private set; }
    public string Domain => Value.Split('@')[1];
    public string LocalPart => Value.Split('@')[0];

    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be null or empty.", nameof(value));

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant().Trim();
    }

    public static Email Create(string value) => new(value);

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    public bool IsCorporateEmail()
    {
        var personalDomains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "aol.com" };
        return !personalDomains.Contains(Domain.ToLowerInvariant());
    }

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Email? left, Email? right) => Equals(left, right);

    public static bool operator !=(Email? left, Email? right) => !Equals(left, right);

    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;
}