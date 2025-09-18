using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

public class RoomAllocationService : IRoomAllocationService
{
    private readonly IChatRoomRepository _chatRoomRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly ILogger<RoomAllocationService> _logger;

    public RoomAllocationService(
        IChatRoomRepository chatRoomRepository,
        IParticipantRepository participantRepository,
        ILogger<RoomAllocationService> logger)
    {
        _chatRoomRepository = chatRoomRepository;
        _participantRepository = participantRepository;
        _logger = logger;
    }

    public async Task<RoomJoinResponse> AllocateParticipantToRoomAsync(SessionId sessionId, string displayName, UserId? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Allocating participant {DisplayName} to room in session {SessionId}",
                displayName, sessionId.Value);

            // Find available room or create new one
            var room = await FindAvailableRoomAsync(sessionId, cancellationToken);
            if (room == null)
            {
                room = await CreateNewRoomAsync(sessionId, cancellationToken);
            }

            // Create participant
            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id,
                SessionId = sessionId,
                DisplayName = displayName,
                UserId = userId,
                Type = userId != null ? ParticipantType.RegisteredUser : ParticipantType.GuestUser,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            await _participantRepository.AddAsync(participant, cancellationToken);

            // Add participant to room
            await AddParticipantToRoomAsync(room.Id, participant, cancellationToken);

            return new RoomJoinResponse
            {
                Success = true,
                RoomId = room.Id.Value.ToString(),
                ParticipantId = participant.Id.ToString(),
                Participant = MapParticipantToResponse(participant),
                Room = await GetRoomDetailsAsync(room.Id, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to allocate participant to room");
            return new RoomJoinResponse
            {
                Success = false,
                ErrorMessage = "Failed to join room"
            };
        }
    }

    public async Task<ChatRoom?> FindAvailableRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rooms = await _chatRoomRepository.GetRoomsBySessionAsync(sessionId, cancellationToken);
            return rooms.FirstOrDefault(r => r.Status == RoomStatus.Active && r.ParticipantCount < r.MaxParticipants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find available room for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<ChatRoom> CreateNewRoomAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new room for session {SessionId}", sessionId.Value);

            var existingRooms = await _chatRoomRepository.GetRoomsBySessionAsync(sessionId, cancellationToken);
            var roomNumber = existingRooms.Count() + 1;

            var room = new ChatRoom
            {
                Id = RoomId.Create(),
                SessionId = sessionId,
                Name = $"Room {roomNumber}",
                MaxParticipants = 6,
                ParticipantCount = 0,
                Status = RoomStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _chatRoomRepository.AddAsync(room, cancellationToken);

            _logger.LogInformation("Created new room {RoomId} for session {SessionId}",
                room.Id.Value, sessionId.Value);

            return room;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new room for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> AddParticipantToRoomAsync(RoomId roomId, Participant participant, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding participant {ParticipantId} to room {RoomId}",
                participant.Id, roomId.Value);

            var room = await _chatRoomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found", roomId.Value);
                return false;
            }

            if (room.ParticipantCount >= room.MaxParticipants)
            {
                _logger.LogWarning("Room {RoomId} is full", roomId.Value);
                return false;
            }

            room.ParticipantCount++;

            if (room.ParticipantCount >= room.MaxParticipants)
            {
                room.Status = RoomStatus.Full;
            }

            await _chatRoomRepository.UpdateAsync(room, cancellationToken);

            _logger.LogInformation("Successfully added participant {ParticipantId} to room {RoomId}",
                participant.Id, roomId.Value);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add participant {ParticipantId} to room {RoomId}",
                participant.Id, roomId.Value);
            throw;
        }
    }

    public async Task<bool> RemoveParticipantFromRoomAsync(RoomId roomId, Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing participant {ParticipantId} from room {RoomId}",
                participantId, roomId.Value);

            var room = await _chatRoomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found", roomId.Value);
                return false;
            }

            if (room.ParticipantCount > 0)
            {
                room.ParticipantCount--;
    
                // If room was full, make it available again
                if (room.Status == RoomStatus.Full)
                {
                    room.Status = RoomStatus.Active;
                }

                await _chatRoomRepository.UpdateAsync(room, cancellationToken);

                _logger.LogInformation("Successfully removed participant {ParticipantId} from room {RoomId}",
                    participantId, roomId.Value);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove participant {ParticipantId} from room {RoomId}",
                participantId, roomId.Value);
            throw;
        }
    }

    public async Task<bool> IsRoomAvailableAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var room = await _chatRoomRepository.GetByIdAsync(roomId, cancellationToken);
            return room != null && room.Status == RoomStatus.Active && room.ParticipantCount < room.MaxParticipants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check room availability for {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<bool> CanJoinRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await IsRoomAvailableAsync(roomId, cancellationToken);
    }

    public async Task<int> GetAvailableSlotsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var room = await _chatRoomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return 0;
            }

            return Math.Max(0, room.MaxParticipants - room.ParticipantCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available slots for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ChatRoomSummaryResponse>> GetAvailableRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rooms = await _chatRoomRepository.GetAvailableRoomsBySessionAsync(sessionId, cancellationToken);
            return rooms.Select(MapToSummaryResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available rooms for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> ConsolidateRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Consolidating rooms for session {SessionId}", sessionId.Value);

            var rooms = await _chatRoomRepository.GetRoomsBySessionAsync(sessionId, cancellationToken);
            var activeRooms = rooms.Where(r => r.Status == RoomStatus.Active).ToList();

            if (activeRooms.Count < 2)
            {
                _logger.LogInformation("Not enough active rooms to consolidate for session {SessionId}", sessionId.Value);
                return true;
            }

            // Simple consolidation logic - close empty rooms
            foreach (var room in activeRooms.Where(r => r.ParticipantCount == 0))
            {
                room.Status = RoomStatus.Closed;
                    await _chatRoomRepository.UpdateAsync(room, cancellationToken);
            }

            _logger.LogInformation("Room consolidation completed for session {SessionId}", sessionId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consolidate rooms for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> UpdateRoomStatusAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var room = await _chatRoomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null) return false;

            // Update status based on participant count
            if (room.ParticipantCount == 0)
            {
                room.Status = RoomStatus.Active;
            }
            else if (room.ParticipantCount >= room.MaxParticipants)
            {
                room.Status = RoomStatus.Full;
            }
            else
            {
                room.Status = RoomStatus.Active;
            }

            await _chatRoomRepository.UpdateAsync(room, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update room status for {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<ChatRoomResponse?> GetRoomDetailsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var room = await _chatRoomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
            {
                return null;
            }

            var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId, cancellationToken);

            return new ChatRoomResponse
            {
                Id = room.Id.Value.ToString(),
                SessionId = room.SessionId.Value.ToString(),
                Name = room.Name,
                ParticipantCount = room.ParticipantCount,
                MaxParticipants = room.MaxParticipants,
                Status = room.Status,
                CreatedAt = room.CreatedAt,
                ClosedAt = room.Status == RoomStatus.Closed ? DateTime.UtcNow : null,
                IsFull = room.ParticipantCount >= room.MaxParticipants,
                HasSpace = room.ParticipantCount < room.MaxParticipants,
                IsActive = room.Status == RoomStatus.Active,
                AvailableSlots = room.MaxParticipants - room.ParticipantCount,
                Participants = participants.Select(MapParticipantToResponse).ToList(),
                RecentMessages = new List<ChatMessageResponse>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room details for {RoomId}", roomId.Value);
            throw;
        }
    }

    private static ChatRoomSummaryResponse MapToSummaryResponse(ChatRoom room)
    {
        return new ChatRoomSummaryResponse
        {
            Id = room.Id.Value.ToString(),
            Name = room.Name,
            ParticipantCount = room.ParticipantCount,
            MaxParticipants = room.MaxParticipants,
            Status = room.Status,
            HasSpace = room.ParticipantCount < room.MaxParticipants,
            AvailableSlots = room.MaxParticipants - room.ParticipantCount
        };
    }

    private static ParticipantResponse MapParticipantToResponse(Participant participant)
    {
        return new ParticipantResponse
        {
            Id = participant.Id.ToString(),
            DisplayName = participant.DisplayName,
            Type = participant.Type,
            IsActive = participant.IsActive,
            JoinedAt = participant.JoinedAt,
            LeftAt = participant.LeftAt
        };
    }

    public async Task<ChatRoomResponse?> GetRoomWithDetailsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await GetRoomDetailsAsync(roomId, cancellationToken);
    }
}