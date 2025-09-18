using Microsoft.EntityFrameworkCore;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Infrastructure.Persistence.Context;

namespace WaglBackend.Infrastructure.Persistence.Repositories;

public class ChatRoomRepository : BaseRepository<ChatRoom>, IChatRoomRepository
{
    public ChatRoomRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ChatRoom?> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken);
    }

    public async Task<ChatRoom?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetAvailableRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       x.Status == RoomStatus.Active &&
                       x.ParticipantCount < x.MaxParticipants)
            .OrderBy(x => x.ParticipantCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetFullRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       (x.Status == RoomStatus.Full ||
                        x.ParticipantCount >= x.MaxParticipants))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetRoomsByStatusAsync(RoomStatus status, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.Status == status)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetActiveRoomsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId && x.Status == RoomStatus.Active)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatRoom?> GetLeastPopulatedRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       x.Status == RoomStatus.Active &&
                       x.ParticipantCount < x.MaxParticipants)
            .OrderBy(x => x.ParticipantCount)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ChatRoom?> GetMostPopulatedRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.ParticipantCount)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetAvailableRoomCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId &&
                           x.Status == RoomStatus.Active &&
                           x.ParticipantCount < x.MaxParticipants, cancellationToken);
    }

    public async Task<int> GetFullRoomCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .CountAsync(x => x.SessionId == sessionId &&
                           (x.Status == RoomStatus.Full ||
                            x.ParticipantCount >= x.MaxParticipants), cancellationToken);
    }

    public async Task<int> GetTotalParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .SumAsync(x => x.ParticipantCount, cancellationToken);
    }

    public async Task<int> GetTotalCapacityAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .SumAsync(x => x.MaxParticipants, cancellationToken);
    }

    public async Task<double> GetAverageOccupancyRateAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var rooms = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (!rooms.Any())
            return 0;

        var totalParticipants = rooms.Sum(x => x.ParticipantCount);
        var totalCapacity = rooms.Sum(x => x.MaxParticipants);

        return totalCapacity > 0 ? (double)totalParticipants / totalCapacity : 0;
    }

    public async Task<bool> HasAvailableRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .AnyAsync(x => x.SessionId == sessionId &&
                          x.Status == RoomStatus.Active &&
                          x.ParticipantCount < x.MaxParticipants, cancellationToken);
    }

    public async Task<bool> AreAllRoomsFullAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var totalRooms = await Query
            .CountAsync(x => x.SessionId == sessionId, cancellationToken);

        if (totalRooms == 0)
            return false;

        var fullRooms = await Query
            .CountAsync(x => x.SessionId == sessionId &&
                           (x.Status == RoomStatus.Full ||
                            x.ParticipantCount >= x.MaxParticipants), cancellationToken);

        return fullRooms == totalRooms;
    }

    public async Task<IEnumerable<ChatRoom>> GetRoomsWithParticipantCountAsync(SessionId sessionId, int participantCount, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId &&
                       x.ParticipantCount == participantCount)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetRoomsNeedingBalancingAsync(SessionId sessionId, int threshold = 2, CancellationToken cancellationToken = default)
    {
        var averageParticipants = await GetAverageParticipantsPerRoomAsync(sessionId, cancellationToken);

        return await Query
            .Where(x => x.SessionId == sessionId &&
                       (x.ParticipantCount > averageParticipants + threshold ||
                        x.ParticipantCount < averageParticipants - threshold))
            .OrderByDescending(x => Math.Abs(x.ParticipantCount - averageParticipants))
            .ToListAsync(cancellationToken);
    }

    public async Task<double> GetAverageParticipantsPerRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var rooms = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (!rooms.Any())
            return 0;

        return rooms.Average(x => x.ParticipantCount);
    }

    public async Task UpdateRoomStatusesAsync(SessionId sessionId, RoomStatus newStatus, CancellationToken cancellationToken = default)
    {
        var rooms = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        foreach (var room in rooms)
        {
            room.Status = newStatus;
        }

        if (rooms.Any())
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ChatRoom?> GetWithParticipantsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    }

    public async Task<ChatRoom?> GetWithMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatRooms
            .Include(r => r.ChatMessages)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    }

    public async Task<ChatRoom?> GetWithParticipantsAndMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await Context.ChatRooms
            .Include(r => r.Participants)
            .Include(r => r.ChatMessages)
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
    }

    public async Task<bool> UpdateParticipantCountAsync(RoomId roomId, int count, CancellationToken cancellationToken = default)
    {
        var room = await GetByRoomIdAsync(roomId, cancellationToken);
        if (room != null)
        {
            room.ParticipantCount = count;
            await Context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }

    public async Task<int> GetTotalParticipantsInSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.SessionId == sessionId)
            .SumAsync(x => x.ParticipantCount, cancellationToken);
    }


    public async Task DeleteBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var rooms = await Query
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        if (rooms.Any())
        {
            DbSet.RemoveRange(rooms);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<ChatRoom>> GetEmptyRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.ParticipantCount == 0)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}