using FluentAssertions;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using Xunit;

namespace WaglBackend.Tests.Unit.Atoms.ValueObjects;

public class TierLevelTests
{
    [Theory]
    [InlineData(AccountTier.Tier1, 1)]
    [InlineData(AccountTier.Tier2, 2)]
    [InlineData(AccountTier.Tier3, 3)]
    public void FromTier_ShouldCreateCorrectTierLevel(AccountTier tier, int expectedLevel)
    {
        // Act
        var tierLevel = TierLevel.FromTier(tier);

        // Assert
        tierLevel.Tier.Should().Be(tier);
        tierLevel.Level.Should().Be(expectedLevel);
        tierLevel.DisplayName.Should().Be(tier.ToString());
    }

    [Theory]
    [InlineData(AccountTier.Tier1, AccountTier.Tier1, true)]
    [InlineData(AccountTier.Tier2, AccountTier.Tier1, true)]
    [InlineData(AccountTier.Tier3, AccountTier.Tier2, true)]
    [InlineData(AccountTier.Tier1, AccountTier.Tier2, false)]
    [InlineData(AccountTier.Tier1, AccountTier.Tier3, false)]
    public void HasAccessToTier_ShouldReturnCorrectResult(AccountTier currentTier, AccountTier requiredTier, bool expectedResult)
    {
        // Arrange
        var tierLevel = TierLevel.FromTier(currentTier);

        // Act
        var result = tierLevel.HasAccessToTier(requiredTier);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(AccountTier.Tier1, 100)]
    [InlineData(AccountTier.Tier2, 500)]
    [InlineData(AccountTier.Tier3, 2000)]
    public void GetHourlyRateLimit_ShouldReturnCorrectLimit(AccountTier tier, int expectedLimit)
    {
        // Arrange
        var tierLevel = TierLevel.FromTier(tier);

        // Act
        var limit = tierLevel.GetHourlyRateLimit();

        // Assert
        limit.Should().Be(expectedLimit);
    }

    [Fact]
    public void GetAvailableFeatures_Tier1_ShouldReturnBasicFeatures()
    {
        // Arrange
        var tierLevel = TierLevel.Tier1;

        // Act
        var features = tierLevel.GetAvailableFeatures();

        // Assert
        features.Should().Contain("BasicAPI");
        features.Should().Contain("StandardSupport");
        features.Should().HaveCount(2);
    }

    [Fact]
    public void GetAvailableFeatures_Tier2_ShouldReturnExtendedFeatures()
    {
        // Arrange
        var tierLevel = TierLevel.Tier2;

        // Act
        var features = tierLevel.GetAvailableFeatures();

        // Assert
        features.Should().Contain("BasicAPI");
        features.Should().Contain("AdvancedAPI");
        features.Should().Contain("PrioritySupport");
        features.Should().Contain("Analytics");
        features.Should().HaveCount(5);
    }

    [Fact]
    public void GetAvailableFeatures_Tier3_ShouldReturnAllFeatures()
    {
        // Arrange
        var tierLevel = TierLevel.Tier3;

        // Act
        var features = tierLevel.GetAvailableFeatures();

        // Assert
        features.Should().Contain("BasicAPI");
        features.Should().Contain("AdvancedAPI");
        features.Should().Contain("PremiumAPI");
        features.Should().Contain("24x7Support");
        features.Should().Contain("CustomIntegrations");
        features.Should().HaveCount(7);
    }

    [Fact]
    public void CompareTo_ShouldWorkCorrectly()
    {
        // Arrange
        var tier1 = TierLevel.Tier1;
        var tier2 = TierLevel.Tier2;
        var tier3 = TierLevel.Tier3;

        // Act & Assert
        (tier1 < tier2).Should().BeTrue();
        (tier2 < tier3).Should().BeTrue();
        (tier3 > tier1).Should().BeTrue();
        (tier2 >= tier1).Should().BeTrue();
        (tier1 <= tier3).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameTier_ShouldReturnTrue()
    {
        // Arrange
        var tier1a = TierLevel.FromTier(AccountTier.Tier2);
        var tier1b = TierLevel.FromTier(AccountTier.Tier2);

        // Act & Assert
        tier1a.Equals(tier1b).Should().BeTrue();
        (tier1a == tier1b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentTier_ShouldReturnFalse()
    {
        // Arrange
        var tier1 = TierLevel.Tier1;
        var tier2 = TierLevel.Tier2;

        // Act & Assert
        tier1.Equals(tier2).Should().BeFalse();
        (tier1 == tier2).Should().BeFalse();
    }
}