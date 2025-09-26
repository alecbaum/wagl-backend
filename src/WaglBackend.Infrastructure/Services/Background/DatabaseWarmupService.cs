using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Services.Background;

/// <summary>
/// Background service to keep database connections warm for Aurora Serverless v2
/// </summary>
public class DatabaseWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseWarmupService> _logger;

    public DatabaseWarmupService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database warmup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Execute a lightweight query every 2 minutes to keep Aurora warm
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Simple query to keep connection alive
                var userCount = await dbContext.Users.CountAsync(stoppingToken);

                _logger.LogDebug("Database warmup ping successful. User count: {UserCount}", userCount);

                // Wait 2 minutes before next warmup
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database warmup failed, retrying in 30 seconds");

                // Wait 30 seconds before retrying on error
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Database warmup service stopped");
    }
}