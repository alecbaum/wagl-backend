using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace WaglBackend.Core.Atoms.ValueObjects;

public class ApiKey : IEquatable<ApiKey>
{
    public string Value { get; private set; }
    public string HashedValue { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ApiKey(string value)
    {
        Value = value;
        HashedValue = BCrypt.Net.BCrypt.HashPassword(value, BCrypt.Net.BCrypt.GenerateSalt(12));
        CreatedAt = DateTime.UtcNow;
    }

    private ApiKey(string value, string hashedValue, DateTime createdAt)
    {
        Value = value;
        HashedValue = hashedValue;
        CreatedAt = createdAt;
    }

    public static ApiKey Create() => new(GenerateSecureKey());

    public static ApiKey FromString(string value) => new(value);

    public static ApiKey FromHashedValue(string value, string hashedValue, DateTime createdAt) 
        => new(value, hashedValue, createdAt);

    public bool Verify(string plainTextKey) => BCrypt.Net.BCrypt.Verify(plainTextKey, HashedValue);

    private static string GenerateSecureKey()
    {
        const int keyLength = 32;
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[keyLength];
        rng.GetBytes(bytes);
        
        var result = new char[keyLength];
        for (int i = 0; i < keyLength; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        
        return $"wagl_{new string(result)}";
    }

    public bool Equals(ApiKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as ApiKey);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(ApiKey? left, ApiKey? right) => Equals(left, right);

    public static bool operator !=(ApiKey? left, ApiKey? right) => !Equals(left, right);

    public override string ToString() => $"ApiKey: {Value[..10]}...";
}