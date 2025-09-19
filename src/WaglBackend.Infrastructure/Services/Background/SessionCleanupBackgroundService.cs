using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services.Background;

public class SessionCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public SessionCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromMinutes(5); // Run cleanup every 5 minutes
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupBackgroundService started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing session cleanup task");
            }

            // Wait for the next cleanup interval
            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        var currentTime = DateTime.UtcNow;
        var cleanupTasks = new List<Task>();

        // Each cleanup task gets its own scope to avoid DbContext threading issues
        // 1. Clean up expired sessions
        cleanupTasks.Add(Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
            await CleanupExpiredSessionsAsync(sessionRepository, currentTime, cancellationToken);
        }, cancellationToken));

        // 2. Clean up inactive participants (disconnected for more than 30 minutes)
        cleanupTasks.Add(Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var participantRepository = scope.ServiceProvider.GetRequiredService<IParticipantRepository>();
            await CleanupInactiveParticipantsAsync(participantRepository, currentTime, cancellationToken);
        }, cancellationToken));

        // 3. Clean up empty rooms
        cleanupTasks.Add(Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var roomRepository = scope.ServiceProvider.GetRequiredService<IChatRoomRepository>();
            var participantRepository = scope.ServiceProvider.GetRequiredService<IParticipantRepository>();
            await CleanupEmptyRoomsAsync(roomRepository, participantRepository, cancellationToken);
        }, cancellationToken));

        // 4. Clean up old messages (older than 7 days for completed sessions)
        cleanupTasks.Add(Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var messageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<IChatSessionRepository>();
            await CleanupOldMessagesAsync(messageRepository, sessionRepository, currentTime, cancellationToken);
        }, cancellationToken));

        // 5. Clean up expired invites
        cleanupTasks.Add(Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var inviteRepository = scope.ServiceProvider.GetRequiredService<ISessionInviteRepository>();
            await CleanupExpiredInvitesAsync(inviteRepository, currentTime, cancellationToken);
        }, cancellationToken));

        // Execute all cleanup tasks concurrently, each with its own DbContext scope
        await Task.WhenAll(cleanupTasks);

        _logger.LogInformation("Session cleanup completed at: {time}", DateTimeOffset.Now);
    }

    private async Task CleanupExpiredSessionsAsync(
        IChatSessionRepository sessionRepository,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find sessions that have ended and are older than 1 hour
            var expiredSessions = await sessionRepository.GetExpiredSessionsAsync(
                currentTime.AddHours(-1), cancellationToken);

            foreach (var session in expiredSessions)
            {
                // Only clean up sessions that are completed or cancelled
                if (session.Status == SessionStatus.Completed || session.Status == SessionStatus.Cancelled)
                {
                    // Mark session for archival instead of deletion
                    // In a real system, you might move to an archive table
                    await sessionRepository.ArchiveSessionAsync(session.Id, cancellationToken);

                    _logger.LogInformation("Archived expired session: {SessionId}", session.Id);
                }
            }

            _logger.LogInformation("Processed {Count} expired sessions", expiredSessions.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
        }
    }

    private async Task CleanupInactiveParticipantsAsync(
        IParticipantRepository participantRepository,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find participants who have been inactive for more than 30 minutes
            var inactiveThreshold = currentTime.AddMinutes(-30);
            var inactiveParticipants = await participantRepository.GetInactiveParticipantsAsync(
                inactiveThreshold, cancellationToken);

            foreach (var participant in inactiveParticipants)
            {
                // Mark participant as left if they haven't already
                if (participant.LeftAt == null)
                {
                    participant.LeftAt = currentTime;
                    participant.IsActive = false;
                    await participantRepository.UpdateAsync(participant, cancellationToken);

                    _logger.LogInformation("Marked inactive participant as left: {ParticipantId}", participant.Id);
                }
            }

            _logger.LogInformation("Processed {Count} inactive participants", inactiveParticipants.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up inactive participants");
        }
    }

    private async Task CleanupEmptyRoomsAsync(
        IChatRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find rooms with no active participants
            var emptyRooms = await roomRepository.GetEmptyRoomsAsync(cancellationToken);

            foreach (var room in emptyRooms)
            {
                // Check if room really has no active participants
                var activeParticipants = await participantRepository.GetActiveParticipantsByRoomAsync(
                    room.Id, cancellationToken);

                if (!activeParticipants.Any())
                {
                    // Mark room as closed
                    room.Status = RoomStatus.Closed;
                    room.EndedAt = DateTime.UtcNow;
                    await roomRepository.UpdateAsync(room, cancellationToken);

                    _logger.LogInformation("Closed empty room: {RoomId}", room.Id);
                }
            }

            _logger.LogInformation("Processed {Count} empty rooms", emptyRooms.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up empty rooms");
        }
    }

    private async Task CleanupOldMessagesAsync(
        IChatMessageRepository messageRepository,
        IChatSessionRepository sessionRepository,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find completed sessions older than 7 days
            var cutoffDate = currentTime.AddDays(-7);
            var oldCompletedSessions = await sessionRepository.GetCompletedSessionsBeforeAsync(
                cutoffDate, cancellationToken);

            int totalMessagesArchived = 0;

            foreach (var session in oldCompletedSessions)
            {
                // Archive messages instead of deleting them
                var archivedCount = await messageRepository.ArchiveMessagesBySessionAsync(
                    session.Id, cancellationToken);

                totalMessagesArchived += archivedCount;

                _logger.LogInformation("Archived {Count} messages for session: {SessionId}",
                    archivedCount, session.Id);
            }

            _logger.LogInformation("Total messages archived: {Count}", totalMessagesArchived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old messages");
        }
    }

    private async Task CleanupExpiredInvitesAsync(
        ISessionInviteRepository inviteRepository,
        DateTime currentTime,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find invites that have expired (older than 24 hours and not used)
            var expiredThreshold = currentTime.AddHours(-24);
            var expiredInvites = await inviteRepository.GetExpiredInvitesAsync(
                expiredThreshold, cancellationToken);

            foreach (var invite in expiredInvites)
            {
                // Mark invite as expired
                invite.IsExpired = true;
                invite.ExpiredAt = currentTime;
                await inviteRepository.UpdateAsync(invite, cancellationToken);

                _logger.LogInformation("Marked invite as expired: {InviteToken}", invite.Token);
            }

            _logger.LogInformation("Processed {Count} expired invites", expiredInvites.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired invites");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupBackgroundService is stopping");
        await base.StopAsync(stoppingToken);
    }
}