using WaglBackend.Core.Atoms.Enums;

namespace WaglBackend.Core.Molecules.DTOs.Response;

public class ApiUsageResponse
{
    public Guid Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public string? IpAddress { get; set; }
    public AccountType AccountType { get; set; }
    public int RequestSize { get; set; }
    public int ResponseSize { get; set; }
}

public class ApiUsageStatsResponse
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int ErrorRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Dictionary<string, int> EndpointUsage { get; set; } = new();
    public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
}

public class RateLimitInfoResponse
{
    public int Limit { get; set; }
    public int Used { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetTime { get; set; }
    public string Period { get; set; } = "hour";
}