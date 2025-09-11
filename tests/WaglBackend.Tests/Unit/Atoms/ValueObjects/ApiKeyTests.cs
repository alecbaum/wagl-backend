using FluentAssertions;
using WaglBackend.Core.Atoms.ValueObjects;
using Xunit;

namespace WaglBackend.Tests.Unit.Atoms.ValueObjects;

public class ApiKeyTests
{
    [Fact]
    public void Create_ShouldGenerateValidApiKey()
    {
        // Act
        var apiKey = ApiKey.Create();

        // Assert
        apiKey.Should().NotBeNull();
        apiKey.Value.Should().NotBeNullOrEmpty();
        apiKey.Value.Should().StartWith("wagl_");
        apiKey.HashedValue.Should().NotBeNullOrEmpty();
        apiKey.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FromString_ShouldCreateApiKeyFromString()
    {
        // Arrange
        var keyValue = "test_api_key_123";

        // Act
        var apiKey = ApiKey.FromString(keyValue);

        // Assert
        apiKey.Should().NotBeNull();
        apiKey.Value.Should().Be(keyValue);
        apiKey.HashedValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Verify_WithCorrectKey_ShouldReturnTrue()
    {
        // Arrange
        var plainTextKey = "test_api_key_123";
        var apiKey = ApiKey.FromString(plainTextKey);

        // Act
        var result = apiKey.Verify(plainTextKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectKey_ShouldReturnFalse()
    {
        // Arrange
        var plainTextKey = "test_api_key_123";
        var wrongKey = "wrong_api_key_123";
        var apiKey = ApiKey.FromString(plainTextKey);

        // Act
        var result = apiKey.Verify(wrongKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var keyValue = "test_api_key_123";
        var apiKey1 = ApiKey.FromString(keyValue);
        var apiKey2 = ApiKey.FromString(keyValue);

        // Act & Assert
        (apiKey1 == apiKey2).Should().BeTrue();
        apiKey1.Equals(apiKey2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var apiKey1 = ApiKey.FromString("test_api_key_123");
        var apiKey2 = ApiKey.FromString("different_api_key_123");

        // Act & Assert
        (apiKey1 == apiKey2).Should().BeFalse();
        apiKey1.Equals(apiKey2).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnMaskedValue()
    {
        // Arrange
        var apiKey = ApiKey.FromString("test_api_key_123");

        // Act
        var result = apiKey.ToString();

        // Assert
        result.Should().StartWith("ApiKey: ");
        result.Should().EndWith("...");
        result.Should().NotContain("test_api_key_123");
    }
}