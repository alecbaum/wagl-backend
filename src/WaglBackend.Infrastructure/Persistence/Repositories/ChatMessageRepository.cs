using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Repositories;

public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(ApplicationDbContext context) : base(context)
    {
    }


    public async Task<IEnumerable<ChatMessage>> GetMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ParticipantId == participantId)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.SentAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetRecentMessagesBySessionAsync(SessionId sessionId, int count, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.SentAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesByRoomPaginatedAsync(RoomId roomId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBySessionPaginatedAsync(SessionId sessionId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAfterAsync(RoomId roomId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId &&
                       x.SentAt > timestamp)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesBeforeAsync(RoomId roomId, DateTime timestamp, int count = 50, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId &&
                       x.SentAt < timestamp)
            .OrderByDescending(x => x.SentAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesByTimeRangeAsync(RoomId roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId &&
                       x.SentAt >= startTime &&
                       x.SentAt <= endTime)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesByContentSearchAsync(RoomId roomId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId &&
                       x.Content.Contains(searchTerm))
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage?> GetLatestMessageByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .OrderByDescending(x => x.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ChatMessage?> GetLatestMessageByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ParticipantId == participantId)
            .OrderByDescending(x => x.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetMessageCountByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.RoomId == roomId, cancellationToken);
    }

    public async Task<int> GetMessageCountBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public async Task<int> GetMessageCountByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.ParticipantId == participantId, cancellationToken);
    }

    public async Task<int> GetMessageCountByTimeRangeAsync(RoomId roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.RoomId == roomId &&
                           x.SentAt >= startTime &&
                           x.SentAt <= endTime, cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMostActiveParticipantMessagesAsync(RoomId roomId, int participantCount, CancellationToken cancellationToken = default)
    {
        var topParticipants = await Query
            .Where(x => x.RoomId == roomId)
            .GroupBy(x => x.ParticipantId)
            .OrderByDescending(g => g.Count())
            .Take(participantCount)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        return await Query
            .Where(x => x.RoomId == roomId &&
                       topParticipants.Contains(x.ParticipantId))
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetMessageCountsByParticipantAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.RoomId == roomId)
            .GroupBy(x => x.ParticipantId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<Dictionary<DateTime, int>> GetMessageCountsByHourAsync(RoomId roomId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.RoomId == roomId &&
                       x.SentAt >= startDate &&
                       x.SentAt <= endDate)
            .ToListAsync(cancellationToken);

        return messages
            .GroupBy(x => new DateTime(x.SentAt.Year, x.SentAt.Month, x.SentAt.Day, x.SentAt.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<double> GetAverageMessageLengthAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.RoomId == roomId)
            .Select(x => x.Content.Length)
            .ToListAsync(cancellationToken);

        return messages.Any() ? messages.Average() : 0;
    }

    public async Task<TimeSpan> GetAverageTimeBetweenMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var timestamps = await Query
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.SentAt)
            .Select(x => x.SentAt)
            .ToListAsync(cancellationToken);

        if (timestamps.Count < 2)
            return TimeSpan.Zero;

        var intervals = new List<TimeSpan>();
        for (int i = 1; i < timestamps.Count; i++)
        {
            intervals.Add(timestamps[i] - timestamps[i - 1]);
        }

        var averageTicks = intervals.Average(ts => ts.Ticks);
        return new TimeSpan((long)averageTicks);
    }

    public async Task<bool> HasMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.RoomId == roomId, cancellationToken);
    }

    public async Task<bool> HasRecentMessagesAsync(RoomId roomId, TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeSpan);
        return await Query
            .AnyAsync(x => x.RoomId == roomId &&
                          x.SentAt >= cutoffTime, cancellationToken);
    }

    public async Task<int> DeleteByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.RoomId == roomId)
            .ToListAsync(cancellationToken);

        if (messages.Any())
        {
            DbSet.RemoveRange(messages);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    public async Task<int> DeleteBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (messages.Any())
        {
            DbSet.RemoveRange(messages);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    public async Task<int> DeleteByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.ParticipantId == participantId)
            .ToListAsync(cancellationToken);

        if (messages.Any())
        {
            DbSet.RemoveRange(messages);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    public async Task<int> DeleteOldMessagesAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.SentAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (messages.Any())
        {
            DbSet.RemoveRange(messages);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await DbSet.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            DbSet.Remove(message);
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<int> DeleteMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.RoomId == roomId)
            .ToListAsync(cancellationToken);

        if (messages.Any())
        {
            DbSet.RemoveRange(messages);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    public async Task<int> ArchiveMessagesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (messages.Any())
        {
            // In this implementation, archiving means deleting the messages
            // If you need to keep archived messages, you would add an IsArchived flag to the entity
            DbSet.RemoveRange(messages);
            await Context.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    public async Task<IEnumerable<ChatMessage>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await GetMessagesByRoomAsync(roomId, cancellationToken);
    }

    // UAI Integration methods
    public async Task<ChatMessage?> GetByExternalMessageIdAsync(string externalMessageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalMessageId))
            return null;

        return await Query
            .FirstOrDefaultAsync(x => x.ExternalMessageId == externalMessageId, cancellationToken);
    }
}