namespace WaglBackend.Core.Molecules.Configurations;

public class RedisConfiguration
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "WaglBackendCache";
    public int DefaultExpirationMinutes { get; set; } = 5;
    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectRetryCount { get; set; } = 3;
    public int Database { get; set; } = 0;
    public bool EnableKeyspaceNotifications { get; set; } = false;
    public string KeyPrefix { get; set; } = "wagl:";
}