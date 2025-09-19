using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

/// <summary>
/// Service for managing system-generated participants (bots and moderators)
/// TODO: Placeholder implementation - UAI doesn't send bot/moderator messages yet
/// </summary>
public class SystemParticipantService : ISystemParticipantService
{
    private readonly IParticipantRepository _participantRepository;
    private readonly ILogger<SystemParticipantService> _logger;

    public SystemParticipantService(
        IParticipantRepository participantRepository,
        ILogger<SystemParticipantService> logger)
    {
        _participantRepository = participantRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a system moderator participant for the session
    /// TODO: Placeholder - for when UAI sends moderator messages
    /// </summary>
    public async Task<Participant> GetOrCreateSystemModeratorAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting or creating system moderator for session {SessionId}", sessionId);

        // Try to find existing system moderator for this session
        var existingModerator = await _participantRepository.GetBySessionAndTypeAsync(
            sessionId,
            ParticipantType.SystemModerator,
            cancellationToken);

        if (existingModerator != null)
        {
            _logger.LogDebug("Found existing system moderator {ParticipantId} for session {SessionId}",
                existingModerator.Id, sessionId);
            return existingModerator;
        }

        // Create new system moderator participant
        // Note: System moderators don't belong to a specific room - they can send to all rooms
        var moderatorParticipant = new Participant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            RoomId = RoomId.From(Guid.Empty), // Special value for session-wide participants
            UserId = null, // System participants don't have user IDs
            DisplayName = GetSystemParticipantDisplayName(ParticipantType.SystemModerator),
            Type = ParticipantType.SystemModerator,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            ConnectionId = null // System participants don't have SignalR connections
        };

        await _participantRepository.AddAsync(moderatorParticipant, cancellationToken);

        _logger.LogInformation("Created new system moderator {ParticipantId} for session {SessionId}",
            moderatorParticipant.Id, sessionId);

        return moderatorParticipant;
    }

    /// <summary>
    /// Gets or creates a bot participant for a specific room
    /// TODO: Placeholder - for when UAI sends bot messages
    /// </summary>
    public async Task<Participant> GetOrCreateBotParticipantAsync(
        SessionId sessionId,
        RoomId roomId,
        string botName = "UAI Bot",
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting or creating bot participant for room {RoomId} in session {SessionId}",
            roomId, sessionId);

        // Try to find existing bot for this room
        var existingBot = await _participantRepository.GetByRoomAndTypeAsync(
            roomId,
            ParticipantType.BotParticipant,
            cancellationToken);

        if (existingBot != null)
        {
            _logger.LogDebug("Found existing bot participant {ParticipantId} for room {RoomId}",
                existingBot.Id, roomId);
            return existingBot;
        }

        // Create new bot participant
        var botParticipant = new Participant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            RoomId = roomId,
            UserId = null, // System participants don't have user IDs
            DisplayName = GetSystemParticipantDisplayName(ParticipantType.BotParticipant, botName),
            Type = ParticipantType.BotParticipant,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            ConnectionId = null // System participants don't have SignalR connections
        };

        await _participantRepository.AddAsync(botParticipant, cancellationToken);

        _logger.LogInformation("Created new bot participant {ParticipantId} ({DisplayName}) for room {RoomId}",
            botParticipant.Id, botParticipant.DisplayName, roomId);

        return botParticipant;
    }

    /// <summary>
    /// Gets all system participants (bots and moderators) for a session
    /// </summary>
    public async Task<IEnumerable<Participant>> GetSystemParticipantsAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all system participants for session {SessionId}", sessionId);

        var systemTypes = new[] { ParticipantType.SystemModerator, ParticipantType.BotParticipant };
        var participants = await _participantRepository.GetBySessionAndTypesAsync(
            sessionId,
            systemTypes,
            cancellationToken);

        _logger.LogDebug("Found {Count} system participants for session {SessionId}",
            participants.Count(), sessionId);

        return participants;
    }

    /// <summary>
    /// Gets all bot participants for a specific room
    /// </summary>
    public async Task<IEnumerable<Participant>> GetBotParticipantsAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting bot participants for room {RoomId}", roomId);

        var botParticipants = await _participantRepository.GetByRoomAndTypeAsync(
            roomId,
            ParticipantType.BotParticipant,
            cancellationToken);

        return botParticipants != null ? new[] { botParticipants } : Enumerable.Empty<Participant>();
    }

    /// <summary>
    /// Checks if a participant is a system participant (bot or moderator)
    /// </summary>
    public async Task<bool> IsSystemParticipantAsync(
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);

        if (participant == null)
        {
            return false;
        }

        return participant.Type == ParticipantType.SystemModerator ||
               participant.Type == ParticipantType.BotParticipant;
    }

    /// <summary>
    /// Removes inactive bot participants from a room
    /// </summary>
    public async Task<int> CleanupInactiveBotParticipantsAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Cleaning up inactive bot participants for room {RoomId}", roomId);

        var botParticipants = await GetBotParticipantsAsync(roomId, cancellationToken);
        var inactiveBots = botParticipants.Where(p => !p.IsActive || p.LeftAt.HasValue);

        int removedCount = 0;
        foreach (var bot in inactiveBots)
        {
            await _participantRepository.DeleteAsync(bot.Id, cancellationToken);
            removedCount++;

            _logger.LogInformation("Removed inactive bot participant {ParticipantId} from room {RoomId}",
                bot.Id, roomId);
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} inactive bot participants from room {RoomId}",
                removedCount, roomId);
        }

        return removedCount;
    }

    /// <summary>
    /// Gets the display name for a system participant type
    /// </summary>
    public string GetSystemParticipantDisplayName(ParticipantType participantType, string? customName = null)
    {
        return participantType switch
        {
            ParticipantType.SystemModerator => "System Moderator",
            ParticipantType.BotParticipant => customName ?? "UAI Bot",
            _ => throw new ArgumentException($"Invalid system participant type: {participantType}", nameof(participantType))
        };
    }
}