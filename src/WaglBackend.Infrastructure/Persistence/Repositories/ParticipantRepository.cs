using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Repositories;

public class ParticipantRepository : BaseRepository<Participant>, IParticipantRepository
{
    public ParticipantRepository(ApplicationDbContext context) : base(context)
    {
    }


    public async Task<Participant?> GetParticipantByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.ConnectionId == connectionId, cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetActiveParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId && x.IsActive)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetActiveParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId && x.IsActive)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Participant?> GetParticipantByUserIdAsync(UserId userId, RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoomId == roomId, cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetByUserIdAndSessionIdAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.UserId == userId && x.SessionId == sessionId)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetParticipantsByTypeAsync(ParticipantType type, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Type == type)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetActiveParticipantsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetParticipantsWithConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ConnectionId != null && x.IsActive)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetParticipantsWithoutConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ConnectionId == null && x.IsActive)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetGuestParticipantsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Type == ParticipantType.GuestUser)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetRegisteredUserParticipantsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Type == ParticipantType.RegisteredUser)
            .OrderByDescending(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetParticipantsByJoinTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.JoinedAt >= startTime && x.JoinedAt <= endTime)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetLongTermParticipantsAsync(SessionId sessionId, TimeSpan minimumDuration, CancellationToken cancellationToken = default)
    {
        var participants = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        var currentTime = DateTime.UtcNow;
        return participants.Where(x =>
        {
            var duration = (x.LeftAt ?? currentTime) - x.JoinedAt;
            return duration >= minimumDuration;
        }).OrderByDescending(x => (x.LeftAt ?? currentTime) - x.JoinedAt);
    }

    public async Task<IEnumerable<Participant>> GetRecentlyLeftParticipantsAsync(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);
        return await Query
            .Where(x => !x.IsActive &&
                       x.LeftAt != null &&
                       x.LeftAt >= cutoffTime)
            .OrderByDescending(x => x.LeftAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetStaleConnectionsAsync(TimeSpan maxIdleTime, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(maxIdleTime);
        return await Query
            .Where(x => x.IsActive &&
                       x.ConnectionId != null &&
                       x.JoinedAt < cutoffTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveParticipantCountAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.RoomId == roomId && x.IsActive, cancellationToken);
    }

    public async Task<int> GetTotalParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public async Task<int> GetActiveParticipantCountBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId && x.IsActive, cancellationToken);
    }

    public async Task<int> GetGuestCountBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId && x.Type == ParticipantType.GuestUser, cancellationToken);
    }

    public async Task<int> GetRegisteredUserCountBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId && x.Type == ParticipantType.RegisteredUser, cancellationToken);
    }

    public async Task<bool> IsUserInSessionAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.UserId == userId && x.SessionId == sessionId && x.IsActive, cancellationToken);
    }

    public async Task<bool> IsUserInRoomAsync(UserId userId, RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.UserId == userId && x.RoomId == roomId && x.IsActive, cancellationToken);
    }

    public async Task<bool> HasActiveConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.ConnectionId == connectionId && x.IsActive, cancellationToken);
    }

    public async Task<Participant?> GetCurrentParticipantForUserAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.UserId == userId && x.SessionId == sessionId && x.IsActive)
            .OrderByDescending(x => x.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<RoomId, int>> GetParticipantCountsByRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId && x.IsActive)
            .GroupBy(x => x.RoomId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<Dictionary<ParticipantType, int>> GetParticipantCountsByTypeAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .GroupBy(x => x.Type)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<Dictionary<DateTime, int>> GetParticipantJoinsByHourAsync(SessionId sessionId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var participants = await Query
            .Where(x => x.SessionId == sessionId &&
                       x.JoinedAt >= startDate &&
                       x.JoinedAt <= endDate)
            .ToListAsync(cancellationToken);

        return participants
            .GroupBy(x => new DateTime(x.JoinedAt.Year, x.JoinedAt.Month, x.JoinedAt.Day, x.JoinedAt.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<TimeSpan> GetAverageSessionDurationAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var participants = await Query
            .Where(x => x.SessionId == sessionId && x.LeftAt != null)
            .ToListAsync(cancellationToken);

        if (!participants.Any())
            return TimeSpan.Zero;

        var durations = participants.Select(x => x.LeftAt!.Value - x.JoinedAt);
        var averageTicks = durations.Average(d => d.Ticks);
        return new TimeSpan((long)averageTicks);
    }

    public async Task<double> GetParticipantRetentionRateAsync(SessionId sessionId, TimeSpan minimumDuration, CancellationToken cancellationToken = default)
    {
        var allParticipants = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (!allParticipants.Any())
            return 0;

        var currentTime = DateTime.UtcNow;
        var retainedParticipants = allParticipants.Count(x =>
        {
            var duration = (x.LeftAt ?? currentTime) - x.JoinedAt;
            return duration >= minimumDuration;
        });

        return (double)retainedParticipants / allParticipants.Count;
    }

    public async Task MarkAllAsLeftBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var participants = await Query
            .Where(x => x.SessionId == sessionId && x.IsActive)
            .ToListAsync(cancellationToken);

        var currentTime = DateTime.UtcNow;
        foreach (var participant in participants)
        {
            participant.IsActive = false;
            participant.LeftAt = currentTime;
            participant.ConnectionId = null;
        }

        if (participants.Any())
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupStaleConnectionsAsync(TimeSpan maxIdleTime, CancellationToken cancellationToken = default)
    {
        var staleParticipants = await GetStaleConnectionsAsync(maxIdleTime, cancellationToken);
        var currentTime = DateTime.UtcNow;

        foreach (var participant in staleParticipants)
        {
            participant.IsActive = false;
            participant.LeftAt = currentTime;
            participant.ConnectionId = null;
        }

        if (staleParticipants.Any())
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> UpdateConnectionIdAsync(Guid participantId, string? connectionId, CancellationToken cancellationToken = default)
    {
        var participant = await DbSet.FindAsync(new object[] { participantId }, cancellationToken);
        if (participant != null)
        {
            participant.ConnectionId = connectionId;
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<bool> MarkParticipantAsLeftAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var participant = await DbSet.FindAsync(new object[] { participantId }, cancellationToken);
        if (participant != null)
        {
            participant.IsActive = false;
            participant.LeftAt = DateTime.UtcNow;
            participant.ConnectionId = null;
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task DeleteBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var participants = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (participants.Any())
        {
            DbSet.RemoveRange(participants);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var participants = await Query
            .Where(x => x.RoomId == roomId)
            .ToListAsync(cancellationToken);

        if (participants.Any())
        {
            DbSet.RemoveRange(participants);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Participant>> GetInactiveParticipantsAsync(DateTime inactiveThreshold, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => !x.IsActive &&
                       x.LeftAt != null &&
                       x.LeftAt <= inactiveThreshold)
            .OrderBy(x => x.LeftAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    // System participant methods for UAI integration
    public async Task<Participant?> GetBySessionAndTypeAsync(SessionId sessionId, ParticipantType participantType, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Type == participantType, cancellationToken);
    }

    public async Task<Participant?> GetByRoomAndTypeAsync(RoomId roomId, ParticipantType participantType, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.Type == participantType, cancellationToken);
    }

    public async Task<IEnumerable<Participant>> GetBySessionAndTypesAsync(SessionId sessionId, IEnumerable<ParticipantType> participantTypes, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId && participantTypes.Contains(x.Type))
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }
}