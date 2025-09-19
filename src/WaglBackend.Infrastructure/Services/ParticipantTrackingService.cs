using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

public class ParticipantTrackingService : IParticipantTrackingService
{
    private readonly IParticipantRepository _participantRepository;
    private readonly ILogger<ParticipantTrackingService> _logger;

    public ParticipantTrackingService(
        IParticipantRepository participantRepository,
        ILogger<ParticipantTrackingService> logger)
    {
        _participantRepository = participantRepository;
        _logger = logger;
    }

    public async Task<Participant> CreateParticipantAsync(RoomId roomId, SessionId sessionId, string displayName, UserId? userId = null, string? connectionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating participant {DisplayName} for room {RoomId} in session {SessionId}",
                displayName, roomId.Value, sessionId.Value);

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                SessionId = sessionId,
                UserId = userId,
                DisplayName = displayName,
                ConnectionId = connectionId,
                Type = userId != null ? ParticipantType.RegisteredUser : ParticipantType.GuestUser,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
            };

            await _participantRepository.AddAsync(participant, cancellationToken);

            _logger.LogInformation("Created participant {ParticipantId} ({DisplayName}) for room {RoomId}",
                participant.Id, displayName, roomId.Value);

            return participant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create participant {DisplayName} for room {RoomId}",
                displayName, roomId.Value);
            throw;
        }
    }

    public async Task<ParticipantResponse?> GetParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
            return participant != null ? MapToResponse(participant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participant {ParticipantId}", participantId);
            throw;
        }
    }

    public async Task<Participant?> GetByIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _participantRepository.GetByIdAsync(participantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participant entity {ParticipantId}", participantId);
            throw;
        }
    }

    public async Task<Participant?> GetParticipantByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _participantRepository.GetParticipantByConnectionIdAsync(connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participant by connection ID {ConnectionId}", connectionId);
            throw;
        }
    }

    public async Task<IEnumerable<ParticipantResponse>> GetParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetParticipantsByRoomAsync(roomId, cancellationToken);
            return participants.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participants for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ParticipantResponse>> GetActiveParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetActiveParticipantsByRoomAsync(roomId, cancellationToken);
            return participants.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active participants for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ParticipantResponse>> GetParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetParticipantsBySessionAsync(sessionId, cancellationToken);
            return participants.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participants for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ParticipantResponse>> GetActiveParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetActiveParticipantsBySessionAsync(sessionId, cancellationToken);
            return participants.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active participants for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> UpdateConnectionIdAsync(Guid participantId, string? connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating connection ID for participant {ParticipantId} to {ConnectionId}",
                participantId, connectionId);

            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
            if (participant == null)
            {
                _logger.LogWarning("Participant {ParticipantId} not found", participantId);
                return false;
            }

            participant.ConnectionId = connectionId;

            await _participantRepository.UpdateAsync(participant, cancellationToken);

            _logger.LogInformation("Successfully updated connection ID for participant {ParticipantId}",
                participantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update connection ID for participant {ParticipantId}",
                participantId);
            throw;
        }
    }

    public async Task<bool> MarkParticipantAsLeftAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Marking participant {ParticipantId} as left", participantId);

            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
            if (participant == null)
            {
                _logger.LogWarning("Participant {ParticipantId} not found", participantId);
                return false;
            }

            participant.IsActive = false;
            participant.LeftAt = DateTime.UtcNow;
            participant.ConnectionId = null;

            await _participantRepository.UpdateAsync(participant, cancellationToken);

            _logger.LogInformation("Successfully marked participant {ParticipantId} as left", participantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark participant {ParticipantId} as left", participantId);
            throw;
        }
    }

    public async Task<bool> MarkParticipantAsActiveAsync(Guid participantId, string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Marking participant {ParticipantId} as active with connection {ConnectionId}",
                participantId, connectionId);

            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
            if (participant == null)
            {
                _logger.LogWarning("Participant {ParticipantId} not found", participantId);
                return false;
            }

            participant.IsActive = true;
            participant.ConnectionId = connectionId;
            participant.LeftAt = null;

            await _participantRepository.UpdateAsync(participant, cancellationToken);

            _logger.LogInformation("Successfully marked participant {ParticipantId} as active", participantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark participant {ParticipantId} as active", participantId);
            throw;
        }
    }

    public async Task<bool> IsParticipantActiveAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
            return participant?.IsActive ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if participant {ParticipantId} is active", participantId);
            throw;
        }
    }

    public async Task<bool> IsUserInSessionAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetByUserIdAndSessionIdAsync(userId, sessionId, cancellationToken);
            return participants.Any(p => p.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} is in session {SessionId}",
                userId.Value, sessionId.Value);
            throw;
        }
    }

    public async Task<int> GetActiveParticipantCountAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetActiveParticipantsByRoomAsync(roomId, cancellationToken);
            return participants.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active participant count for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<int> GetTotalParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetParticipantsBySessionAsync(sessionId, cancellationToken);
            return participants.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total participant count for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ParticipantResponse>> GetParticipantsByTypeAsync(ParticipantType type, CancellationToken cancellationToken = default)
    {
        try
        {
            var participants = await _participantRepository.GetParticipantsByTypeAsync(type, cancellationToken);
            return participants.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participants by type {Type}", type);
            throw;
        }
    }

    private static ParticipantResponse MapToResponse(Participant participant)
    {
        return new ParticipantResponse
        {
            Id = participant.Id.ToString(),
            RoomId = participant.RoomId.Value.ToString(),
            SessionId = participant.SessionId.Value.ToString(),
            UserId = participant.UserId?.Value.ToString(),
            DisplayName = participant.DisplayName,
            Type = participant.Type,
            IsRegisteredUser = participant.UserId != null,
            IsGuest = participant.UserId == null,
            IsConnected = !string.IsNullOrEmpty(participant.ConnectionId),
            Duration = participant.LeftAt.HasValue ? participant.LeftAt.Value - participant.JoinedAt :
                      participant.IsActive ? DateTime.UtcNow - participant.JoinedAt : null,
            MessageCount = 0, // TODO: Implement message count calculation
            IsActive = participant.IsActive,
            JoinedAt = participant.JoinedAt,
            LeftAt = participant.LeftAt
        };
    }

    public async Task<int> GetTotalParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _participantRepository.GetActiveParticipantCountAsync(roomId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total participants for room: {RoomId}", roomId);
            return 0;
        }
    }
}