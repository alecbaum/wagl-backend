using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Atoms.ValueObjects;

public class TierLevel : IEquatable<TierLevel>, IComparable<TierLevel>
{
    public AccountTier Tier { get; private set; }
    public int Level => (int)Tier;
    public string DisplayName => Tier.ToString();

    private TierLevel(AccountTier tier)
    {
        Tier = tier;
    }

    public static TierLevel FromTier(AccountTier tier) => new(tier);

    public static TierLevel Tier1 => new(AccountTier.Tier1);
    public static TierLevel Tier2 => new(AccountTier.Tier2);
    public static TierLevel Tier3 => new(AccountTier.Tier3);

    public bool HasAccessToTier(AccountTier requiredTier) => Level >= (int)requiredTier;

    public bool CanAccessFeature(TierLevel requiredLevel) => Level >= requiredLevel.Level;

    public string[] GetAvailableFeatures()
    {
        return Tier switch
        {
            AccountTier.Tier1 => new[] { "BasicAPI", "StandardSupport" },
            AccountTier.Tier2 => new[] { "BasicAPI", "AdvancedAPI", "PrioritySupport", "Analytics" },
            AccountTier.Tier3 => new[] { "BasicAPI", "AdvancedAPI", "PremiumAPI", "24x7Support", "Analytics", "CustomIntegrations" },
            _ => Array.Empty<string>()
        };
    }

    public int GetHourlyRateLimit()
    {
        return Tier switch
        {
            AccountTier.Tier1 => 100,
            AccountTier.Tier2 => 500,
            AccountTier.Tier3 => 2000,
            _ => 0
        };
    }

    public bool Equals(TierLevel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Tier == other.Tier;
    }

    public override bool Equals(object? obj) => Equals(obj as TierLevel);

    public override int GetHashCode() => Tier.GetHashCode();

    public int CompareTo(TierLevel? other)
    {
        if (other is null) return 1;
        return Level.CompareTo(other.Level);
    }

    public static bool operator ==(TierLevel? left, TierLevel? right) => Equals(left, right);
    public static bool operator !=(TierLevel? left, TierLevel? right) => !Equals(left, right);
    public static bool operator >(TierLevel? left, TierLevel? right) => left?.CompareTo(right) > 0;
    public static bool operator <(TierLevel? left, TierLevel? right) => left?.CompareTo(right) < 0;
    public static bool operator >=(TierLevel? left, TierLevel? right) => left?.CompareTo(right) >= 0;
    public static bool operator <=(TierLevel? left, TierLevel? right) => left?.CompareTo(right) <= 0;

    public override string ToString() => DisplayName;
}