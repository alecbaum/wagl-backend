using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services.Background;

public class SessionSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionSchedulerBackgroundService> _logger;
    private readonly TimeSpan _schedulerInterval;

    public SessionSchedulerBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SessionSchedulerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _schedulerInterval = TimeSpan.FromMinutes(1); // Check for scheduled sessions every minute
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionSchedulerBackgroundService started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndStartScheduledSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing session scheduler task");
            }

            // Wait for the next scheduler interval
            await Task.Delay(_schedulerInterval, stoppingToken);
        }
    }

    private async Task CheckAndStartScheduledSessionsAsync(CancellationToken cancellationToken)
    {
        var currentTime = DateTime.UtcNow;

        try
        {
            // First, check for sessions to start (separate scope)
            await CheckSessionsToStartAsync(currentTime, cancellationToken);

            // Then, check for sessions to end (separate scope to avoid DbContext conflicts)
            await CheckSessionsToEndAsync(currentTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking scheduled sessions");
        }
    }

    private async Task CheckSessionsToStartAsync(DateTime currentTime, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
        var chatSessionService = scope.ServiceProvider.GetRequiredService<IChatSessionService>();
        var roomAllocationService = scope.ServiceProvider.GetRequiredService<IRoomAllocationService>();

        try
        {
            // Find sessions that are scheduled to start now or in the past
            var scheduledSessions = await sessionRepository.GetScheduledSessionsAsync(cancellationToken);

            var sessionsToStart = scheduledSessions.Where(session =>
                session.Status == SessionStatus.Scheduled &&
                session.ScheduledStartTime <= currentTime.AddMinutes(1)) // Start sessions within 1 minute of scheduled time
                .ToList();

            _logger.LogInformation("Found {Count} sessions ready to start", sessionsToStart.Count);

            foreach (var session in sessionsToStart)
            {
                try
                {
                    await StartScheduledSessionAsync(session, chatSessionService, roomAllocationService, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start scheduled session: {SessionId}", session.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sessions to start");
        }
    }

    private async Task CheckSessionsToEndAsync(DateTime currentTime, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
        var chatSessionService = scope.ServiceProvider.GetRequiredService<IChatSessionService>();

        try
        {
            await CheckAndEndExpiredSessionsAsync(sessionRepository, chatSessionService, currentTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking sessions to end");
        }
    }

    private async Task StartScheduledSessionAsync(
        Core.Atoms.Entities.ChatSession session,
        IChatSessionService chatSessionService,
        IRoomAllocationService roomAllocationService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting scheduled session: {SessionId} at {StartTime}",
                session.Id, DateTime.UtcNow);

            // Start the session
            var sessionStarted = await chatSessionService.StartSessionAsync(session.Id, cancellationToken);

            if (sessionStarted)
            {
                // Pre-allocate rooms for the session
                await roomAllocationService.PreAllocateRoomsForSessionAsync(session.Id, cancellationToken);

                _logger.LogInformation("Successfully started session: {SessionId}", session.Id);
            }
            else
            {
                _logger.LogWarning("Failed to start session: {SessionId}", session.Id);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot start session {SessionId}: {Message}", session.Id, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error starting session: {SessionId}", session.Id);
            throw;
        }
    }

    private async Task CheckAndEndExpiredSessionsAsync(
        IChatSessionRepository sessionRepository,
        IChatSessionService chatSessionService,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find active sessions that have exceeded their duration
            var activeSessions = await sessionRepository.GetActiveSessionsAsync(cancellationToken);

            var expiredSessions = activeSessions.Where(session =>
                session.Status == SessionStatus.Active &&
                session.StartedAt.HasValue &&
                session.StartedAt.Value.Add(session.Duration) <= currentTime)
                .ToList();

            _logger.LogInformation("Found {Count} sessions that have exceeded their duration", expiredSessions.Count);

            foreach (var session in expiredSessions)
            {
                try
                {
                    _logger.LogInformation("Auto-ending expired session: {SessionId} (started at {StartTime}, duration {Duration})",
                        session.Id, session.StartedAt, session.Duration);

                    await chatSessionService.EndSessionAsync(session.Id, cancellationToken);

                    _logger.LogInformation("Successfully ended expired session: {SessionId}", session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to end expired session: {SessionId}", session.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for expired sessions");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionSchedulerBackgroundService is stopping");
        await base.StopAsync(stoppingToken);
    }
}

/// <summary>
/// Extension methods for room allocation service used by the scheduler
/// </summary>
public static class RoomAllocationServiceExtensions
{
    /// <summary>
    /// Pre-allocates rooms for a session to ensure they're ready when participants join
    /// </summary>
    public static async Task PreAllocateRoomsForSessionAsync(
        this IRoomAllocationService roomAllocationService,
        Core.Atoms.ValueObjects.SessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        // This method would be implemented in the actual service
        // For now, we'll just log that pre-allocation would happen here
        await Task.CompletedTask;
    }
}