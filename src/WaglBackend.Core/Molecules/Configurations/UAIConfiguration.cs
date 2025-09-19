namespace WaglBackend.Core.Molecules.Configurations;

/// <summary>
/// Configuration settings for UAI (Unanimous AI) integration
///
/// Room Assignment Strategy:
/// - Room 0: Reserved exclusively for health checks
/// - Rooms 1, 2, 3: Used for actual chat rooms (distributed via hash)
/// </summary>
public class UAIConfiguration
{
    public const string SectionName = "UAI";

    public string BaseUrl { get; set; } = string.Empty;
    public string TestSessionId { get; set; } = "54.196.26.13";
    public int[] AvailableRooms { get; set; } = { 1, 2, 3 };
    public int HealthCheckRoom { get; set; } = 0;
    public string DefaultUrlParams { get; set; } = "?source=web&version=1.0";
    public int TimeoutMs { get; set; } = 5000;
    public bool EnableIntegration { get; set; } = true;
    public UAIRetryPolicy RetryPolicy { get; set; } = new();
    public UAIEndpoints Endpoints { get; set; } = new();
}

public class UAIRetryPolicy
{
    public int MaxAttempts { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 30000;
    public bool EnableExponentialBackoff { get; set; } = true;
    public bool EnableJitter { get; set; } = true;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerSamplingDurationMs { get; set; } = 60000;
    public int CircuitBreakerMinimumThroughput { get; set; } = 3;
    public int CircuitBreakerDurationOfBreakMs { get; set; } = 30000;

    // Legacy property for backward compatibility
    public int BackoffMs
    {
        get => BaseDelayMs;
        set => BaseDelayMs = value;
    }
}

public class UAIEndpoints
{
    public string Health { get; set; } = "/api/health";
    public string MessageSend { get; set; } = "/api/message/send";
    public string UserConnect { get; set; } = "/api/user/connect";
    public string UserDisconnect { get; set; } = "/api/user/disconnect";
}