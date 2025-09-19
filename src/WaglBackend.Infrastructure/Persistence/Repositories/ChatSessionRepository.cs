using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Repositories;

public class ChatSessionRepository : BaseRepository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ChatSession?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsByStatusAsync(SessionStatus status, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == status)
            .OrderBy(x => x.ScheduledStartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetScheduledSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == SessionStatus.Scheduled)
            .OrderBy(x => x.ScheduledStartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == SessionStatus.Active)
            .OrderBy(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetEndedSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == SessionStatus.Ended)
            .OrderByDescending(x => x.EndedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetScheduledSessionsDueForStartAsync(DateTime currentTime, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == SessionStatus.Scheduled &&
                       x.ScheduledStartTime <= currentTime)
            .OrderBy(x => x.ScheduledStartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsDueForEndAsync(DateTime currentTime, CancellationToken cancellationToken = default)
    {
        // Get all active sessions and filter client-side due to EF translation limitations
        var activeSessions = await Query
            .Where(x => x.Status == SessionStatus.Active &&
                       x.StartedAt != null)
            .OrderBy(x => x.StartedAt)
            .ToListAsync(cancellationToken);

        return activeSessions
            .Where(x => x.StartedAt!.Value.Add(x.Duration) <= currentTime);
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ScheduledStartTime >= startDate &&
                       x.ScheduledStartTime <= endDate)
            .OrderBy(x => x.ScheduledStartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        var scheduledCutoff = currentTime.AddHours(-24);
        var activeCutoff = currentTime.AddHours(-1);

        // Get sessions that need client-side evaluation for duration calculation
        var activeSessions = await Query
            .Where(x => x.Status == SessionStatus.Active &&
                       x.StartedAt != null)
            .ToListAsync(cancellationToken);

        var scheduledExpiredSessions = await Query
            .Where(x => x.Status == SessionStatus.Scheduled &&
                       x.ScheduledStartTime < scheduledCutoff)
            .ToListAsync(cancellationToken);

        // Filter active sessions client-side where EF can't translate the Add operation
        var activeExpiredSessions = activeSessions
            .Where(x => x.StartedAt!.Value.Add(x.Duration) < activeCutoff);

        return scheduledExpiredSessions.Concat(activeExpiredSessions);
    }

    public async Task<int> GetActiveSessionCountAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.Status == SessionStatus.Active, cancellationToken);
    }

    public async Task<int> GetScheduledSessionCountAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.Status == SessionStatus.Scheduled, cancellationToken);
    }

    public async Task<bool> HasActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.Status == SessionStatus.Active, cancellationToken);
    }

    public async Task<bool> HasScheduledSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.Status == SessionStatus.Scheduled, cancellationToken);
    }

    public async Task DeleteExpiredSessionsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        var expiredSessions = await Query
            .Where(x => x.Status == SessionStatus.Ended &&
                       x.EndedAt != null &&
                       x.EndedAt.Value < cutoffTime)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Any())
        {
            DbSet.RemoveRange(expiredSessions);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ChatSession?> GetWithRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatSessions
            .Include(x => x.ChatRooms)
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<ChatSession?> GetWithRoomsAndParticipantsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatSessions
            .Include(x => x.ChatRooms)
            .ThenInclude(r => r.Participants)
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetSessionsByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatSessions
            .Where(s => s.ChatRooms.Any(r => r.Participants.Any(p => p.UserId == userId)))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveSessionAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatSessions
            .Where(s => s.Status == SessionStatus.Active)
            .AnyAsync(s => s.ChatRooms.Any(r => r.Participants.Any(p => p.UserId == userId && p.IsActive)), cancellationToken);
    }

    public async Task<int> GetActiveParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Context.Participants
            .CountAsync(p => p.SessionId == sessionId && p.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<ChatSession>> GetExpiredSessionsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        // Get sessions that need client-side evaluation for duration calculation
        var activeSessions = await Query
            .Where(x => x.Status == SessionStatus.Active &&
                       x.StartedAt != null)
            .ToListAsync(cancellationToken);

        var scheduledExpiredSessions = await Query
            .Where(x => x.Status == SessionStatus.Scheduled &&
                       x.ScheduledStartTime < cutoffTime)
            .ToListAsync(cancellationToken);

        // Filter active sessions client-side where EF can't translate the Add operation
        var activeExpiredSessions = activeSessions
            .Where(x => x.StartedAt!.Value.Add(x.Duration) < cutoffTime);

        return scheduledExpiredSessions.Concat(activeExpiredSessions);
    }

    public async Task<IEnumerable<ChatSession>> GetCompletedSessionsBeforeAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == SessionStatus.Ended &&
                       x.EndedAt != null &&
                       x.EndedAt.Value < cutoffDate)
            .OrderBy(x => x.EndedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task ArchiveSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetBySessionIdAsync(sessionId, cancellationToken);
        if (session != null)
        {
            // Mark session as archived by updating its status
            session.Status = SessionStatus.Ended;
            if (session.EndedAt == null)
            {
                session.EndedAt = DateTime.UtcNow;
            }
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

}