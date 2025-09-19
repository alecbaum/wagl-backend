using Microsoft.Extensions.Logging;

namespace WaglBackend.Infrastructure.Services.Logging;

public class LoggingDemoService
{
    private readonly ILogger<LoggingDemoService> _logger;

    public LoggingDemoService(ILogger<LoggingDemoService> logger)
    {
        _logger = logger;
    }

    public void DemonstrateDatabaseLogging()
    {
        _logger.LogInformation("This is an informational message - will not be stored in database");

        _logger.LogWarning("This is a warning message that will be stored in the database with structured properties: {Property1}, {Property2}",
            "WarningValue", DateTime.UtcNow);

        _logger.LogError("This is an error message that will be stored in the database: {ErrorCode}", "UAI_CONNECTION_FAILED");

        try
        {
            throw new InvalidOperationException("This is a test exception for database logging");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during database logging demo with context: {Context}, {UserId}",
                "DemoContext", Guid.NewGuid());
        }

        _logger.LogInformation("Database logging demonstration completed - only warnings and errors above were stored in database");
    }
}