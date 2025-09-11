namespace WaglBackend.Core.Molecules.Configurations;

public class DatabaseConfiguration
{
    public const string SectionName = "Database";
    
    public string PostgreSQLConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableServiceProviderCaching { get; set; } = true;
    public bool EnableQuerySplittingBehavior { get; set; } = false;
}