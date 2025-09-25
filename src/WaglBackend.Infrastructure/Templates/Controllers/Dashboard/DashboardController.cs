using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using WaglBackend.Infrastructure.Templates.Authorization;

namespace WaglBackend.Infrastructure.Templates.Controllers.Dashboard;

/// <summary>
/// Dashboard statistics and overview endpoints
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = ChatAuthorizationPolicies.ChatAccess)]
[ApiController]
[Route("api/v{version:apiVersion}/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IChatSessionService _chatSessionService;
    private readonly IParticipantTrackingService _participantTrackingService;

    public DashboardController(
        IChatSessionService chatSessionService,
        IParticipantTrackingService participantTrackingService,
        ILogger<DashboardController> logger) : base(logger)
    {
        _chatSessionService = chatSessionService;
        _participantTrackingService = participantTrackingService;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDashboardStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get active sessions
            var activeSessions = await _chatSessionService.GetActiveSessionsAsync(cancellationToken);
            var activeSessionCount = activeSessions.Count();

            // Get scheduled sessions
            var scheduledSessions = await _chatSessionService.GetScheduledSessionsAsync(cancellationToken);
            var scheduledSessionCount = scheduledSessions.Count();

            // Calculate total sessions (this would ideally come from a dedicated repository method)
            var totalSessions = activeSessionCount + scheduledSessionCount;

            // Get active participants (estimated - would need proper user tracking)
            var activeUsers = 0; // TODO: Implement proper user activity tracking
            var totalUsers = 0; // TODO: Implement proper user count from Identity

            // For now, estimate based on active sessions
            foreach (var session in activeSessions)
            {
                var participantCount = await _chatSessionService.GetActiveParticipantCountAsync(
                    Core.Atoms.ValueObjects.SessionId.From(session.Id),
                    cancellationToken);
                activeUsers += participantCount;
            }

            var stats = new
            {
                totalSessions = totalSessions,
                activeSessions = activeSessionCount,
                scheduledSessions = scheduledSessionCount,
                totalUsers = totalUsers,
                activeUsers = activeUsers,
                lastUpdated = DateTime.UtcNow
            };

            Logger.LogInformation("Dashboard stats retrieved: {ActiveSessions} active, {ScheduledSessions} scheduled, {ActiveUsers} active users",
                activeSessionCount, scheduledSessionCount, activeUsers);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve dashboard statistics" });
        }
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetSystemHealth(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic health checks
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                uptime = Environment.TickCount64 / 1000, // seconds since startup
                services = new
                {
                    database = "healthy", // TODO: Add actual DB health check
                    redis = "healthy",    // TODO: Add actual Redis health check
                    signalr = "healthy"   // TODO: Add actual SignalR health check
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking system health");
            return StatusCode(500, new {
                status = "unhealthy",
                error = "HEALTH_CHECK_FAILED",
                message = "System health check failed"
            });
        }
    }
}