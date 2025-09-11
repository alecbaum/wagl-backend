namespace WaglBackend.Core.Molecules.Interfaces;

public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? CacheDuration { get; }
    bool ShouldCache { get; }
}