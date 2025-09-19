using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;

namespace WaglBackend.Domain.Organisms.Services;

/// <summary>
/// Service for integrating with UAI (Unanimous AI) system
/// Handles outbound communication to UAI endpoints
/// </summary>
public interface IUAIIntegrationService
{
    /// <summary>
    /// Check if UAI service is healthy and responsive
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a user message to UAI for processing
    /// Maps to UAI /api/message/send endpoint
    /// </summary>
    Task<bool> SendMessageAsync(ChatMessage message, string uaiSessionId, int uaiRoomNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify UAI that a user connected to a room
    /// Maps to UAI /api/user/connect endpoint
    /// </summary>
    Task<bool> NotifyUserConnectAsync(Participant participant, string uaiSessionId, int uaiRoomNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify UAI that a user disconnected from a room
    /// Maps to UAI /api/user/disconnect endpoint
    /// </summary>
    Task<bool> NotifyUserDisconnectAsync(Participant participant, string uaiSessionId, int uaiRoomNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert SessionId to UAI session identifier (IP address)
    /// For now, maps all sessions to test session "54.196.26.13"
    /// </summary>
    string GetUAISessionId(SessionId sessionId);

    /// <summary>
    /// Convert RoomId to UAI room number (1, 2, or 3)
    /// Maps room to one of the 3 available test rooms (excludes room 0 which is reserved for health checks)
    /// </summary>
    int GetUAIRoomNumber(RoomId roomId);

    /// <summary>
    /// Get UAI room number for health checks (room 0)
    /// Room 0 is reserved exclusively for health checks
    /// </summary>
    int GetHealthCheckRoomNumber();

    /// <summary>
    /// Convert Participant.Id to UAI user ID (long number)
    /// </summary>
    long GetUAIUserId(Guid participantId);
}