using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Domain.Organisms.Services;

/// <summary>
/// Service for managing system-generated participants (bots and moderators)
/// TODO: Placeholder interface - UAI doesn't send bot/moderator messages yet
/// </summary>
public interface ISystemParticipantService
{
    /// <summary>
    /// Gets or creates a system moderator participant for the session
    /// TODO: Placeholder - for when UAI sends moderator messages
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The system moderator participant</returns>
    Task<Participant> GetOrCreateSystemModeratorAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a bot participant for a specific room
    /// TODO: Placeholder - for when UAI sends bot messages
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="roomId">The room ID</param>
    /// <param name="botName">The bot's display name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The bot participant</returns>
    Task<Participant> GetOrCreateBotParticipantAsync(
        SessionId sessionId,
        RoomId roomId,
        string botName = "UAI Bot",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all system participants (bots and moderators) for a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of system participants</returns>
    Task<IEnumerable<Participant>> GetSystemParticipantsAsync(
        SessionId sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bot participants for a specific room
    /// </summary>
    /// <param name="roomId">The room ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bot participants in the room</returns>
    Task<IEnumerable<Participant>> GetBotParticipantsAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a participant is a system participant (bot or moderator)
    /// </summary>
    /// <param name="participantId">The participant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the participant is a system participant</returns>
    Task<bool> IsSystemParticipantAsync(
        Guid participantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes inactive bot participants from a room
    /// </summary>
    /// <param name="roomId">The room ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of participants removed</returns>
    Task<int> CleanupInactiveBotParticipantsAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the display name for a system participant type
    /// </summary>
    /// <param name="participantType">The participant type</param>
    /// <param name="customName">Optional custom name for bots</param>
    /// <returns>The display name</returns>
    string GetSystemParticipantDisplayName(ParticipantType participantType, string? customName = null);
}