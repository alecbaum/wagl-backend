namespace WaglBackend.Core.Molecules.Interfaces;

public interface IRateLimited
{
    int GetRateLimit();
    TimeSpan GetRateLimitWindow();
    string GetRateLimitIdentifier();
}