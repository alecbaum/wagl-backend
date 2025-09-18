using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

public class ChatSessionService : IChatSessionService
{
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IChatRoomRepository _chatRoomRepository;
    private readonly ILogger<ChatSessionService> _logger;

    public ChatSessionService(
        IChatSessionRepository chatSessionRepository,
        IChatRoomRepository chatRoomRepository,
        ILogger<ChatSessionService> logger)
    {
        _chatSessionRepository = chatSessionRepository;
        _chatRoomRepository = chatRoomRepository;
        _logger = logger;
    }

    public async Task<ChatSessionResponse> CreateSessionAsync(ChatSessionRequest request, UserId? createdByUserId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new chat session: {SessionName}", request.Name);

            var session = new ChatSession
            {
                Id = SessionId.Create(),
                Name = request.Name,
                ScheduledStartTime = request.ScheduledStartTime,
                Duration = TimeSpan.FromMinutes(request.DurationMinutes),
                MaxParticipants = request.MaxParticipants,
                Status = SessionStatus.Scheduled,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _chatSessionRepository.AddAsync(session, cancellationToken);

            // Create initial chat rooms for the session (6 rooms with 6 participants each by default)
            var roomsToCreate = Math.Ceiling((double)request.MaxParticipants / 6);
            for (int i = 0; i < roomsToCreate; i++)
            {
                var room = new ChatRoom
                {
                    Id = RoomId.Create(),
                    SessionId = session.Id,
                    Name = $"Room {i + 1}",
                    MaxParticipants = 6,
                    ParticipantCount = 0,
                    Status = RoomStatus.Waiting,
                    CreatedAt = DateTime.UtcNow
                };

                await _chatRoomRepository.AddAsync(room, cancellationToken);
            }

            _logger.LogInformation("Created chat session {SessionId} with {RoomCount} rooms",
                session.Id.Value, roomsToCreate);

            return MapToResponse(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat session: {SessionName}", request.Name);
            throw;
        }
    }

    public async Task<ChatSessionResponse?> GetSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            return session != null ? MapToResponse(session) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<ChatSessionResponse?> GetSessionWithDetailsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null) return null;

            var response = MapToResponse(session);

            // Load room details - using a stub implementation since we don't have the exact repository method
            response.TotalRooms = 6; // Default room count
            response.ActiveParticipants = 0; // Stub value

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session with details {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ChatSessionSummaryResponse>> GetScheduledSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Stub implementation - in real implementation would filter by status
            var sessions = await _chatSessionRepository.GetAllAsync(cancellationToken);
            var scheduledSessions = sessions.Where(s => s.Status == SessionStatus.Scheduled);
            return scheduledSessions.Select(MapToSummaryResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled sessions");
            throw;
        }
    }

    public async Task<IEnumerable<ChatSessionSummaryResponse>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Stub implementation - in real implementation would filter by status
            var sessions = await _chatSessionRepository.GetAllAsync(cancellationToken);
            var activeSessions = sessions.Where(s => s.Status == SessionStatus.Active);
            return activeSessions.Select(MapToSummaryResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions");
            throw;
        }
    }

    public async Task<IEnumerable<ChatSessionSummaryResponse>> GetSessionsByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Stub implementation - in real implementation would filter by user
            var sessions = await _chatSessionRepository.GetAllAsync(cancellationToken);
            var userSessions = sessions.Where(s => s.CreatedByUserId == userId);
            return userSessions.Select(MapToSummaryResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions by user {UserId}", userId.Value);
            throw;
        }
    }

    public async Task<bool> StartSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting session {SessionId}", sessionId.Value);

            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId.Value);
                return false;
            }

            if (session.Status != SessionStatus.Scheduled)
            {
                _logger.LogWarning("Cannot start session {SessionId} with status {Status}",
                    sessionId.Value, session.Status);
                return false;
            }

            session.Status = SessionStatus.Active;
            session.StartedAt = DateTime.UtcNow;

            await _chatSessionRepository.UpdateAsync(session, cancellationToken);

            _logger.LogInformation("Successfully started session {SessionId}", sessionId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> EndSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Ending session {SessionId}", sessionId.Value);

            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId.Value);
                return false;
            }

            if (session.Status != SessionStatus.Active)
            {
                _logger.LogWarning("Cannot end session {SessionId} with status {Status}",
                    sessionId.Value, session.Status);
                return false;
            }

            session.Status = SessionStatus.Ended;
            session.EndedAt = DateTime.UtcNow;

            await _chatSessionRepository.UpdateAsync(session, cancellationToken);

            _logger.LogInformation("Successfully ended session {SessionId}", sessionId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> CancelSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling session {SessionId}", sessionId.Value);

            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId.Value);
                return false;
            }

            if (session.Status == SessionStatus.Ended || session.Status == SessionStatus.Cancelled)
            {
                _logger.LogWarning("Cannot cancel session {SessionId} with status {Status}",
                    sessionId.Value, session.Status);
                return false;
            }

            session.Status = SessionStatus.Cancelled;
            await _chatSessionRepository.UpdateAsync(session, cancellationToken);

            _logger.LogInformation("Successfully cancelled session {SessionId}", sessionId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> CanStartSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null) return false;

            return session.CanStart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if session can start {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> IsSessionActiveAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            return session?.IsActive ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if session {SessionId} is active", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> UpdateSessionStatusAsync(SessionId sessionId, SessionStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null) return false;

            session.Status = status;

            if (status == SessionStatus.Active && session.StartedAt == null)
            {
                session.StartedAt = DateTime.UtcNow;
            }
            else if (status == SessionStatus.Ended && session.EndedAt == null)
            {
                session.EndedAt = DateTime.UtcNow;
            }

            await _chatSessionRepository.UpdateAsync(session, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session status {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<int> GetActiveParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Stub implementation - would need to sum participants from all rooms
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active participant count {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _chatSessionRepository.GetAllAsync(cancellationToken);
            var expiredSessions = sessions.Where(s => s.IsExpired);

            foreach (var session in expiredSessions)
            {
                await _chatSessionRepository.DeleteAsync(session.Id, cancellationToken);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expired sessions");
            throw;
        }
    }

    private static ChatSessionSummaryResponse MapToSummaryResponse(ChatSession session)
    {
        return new ChatSessionSummaryResponse
        {
            Id = session.Id.Value.ToString(),
            Name = session.Name,
            ScheduledStartTime = session.ScheduledStartTime,
            Status = session.Status,
            TotalRooms = 6, // Default room count
            ActiveParticipants = 0, // Stub value
            CanStart = session.CanStart
        };
    }

    private static ChatSessionResponse MapToResponse(ChatSession session)
    {
        return new ChatSessionResponse
        {
            Id = session.Id.Value.ToString(),
            Name = session.Name,
            ScheduledStartTime = session.ScheduledStartTime,
            DurationMinutes = (int)session.Duration.TotalMinutes,
            MaxParticipants = session.MaxParticipants,
            MaxParticipantsPerRoom = session.MaxParticipantsPerRoom,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            CreatedByUserId = session.CreatedByUserId?.Value.ToString(),
            CreatedBy = session.CreatedByUserId?.Value.ToString(), // TODO: Map to actual user name
            IsPublic = true, // TODO: Implement IsPublic logic
            ScheduledEndTime = session.ScheduledEndTime,
            IsExpired = session.IsExpired,
            CanStart = session.CanStart,
            IsActive = session.IsActive,
            TotalRooms = 6, // Default room count
            ActiveParticipants = 0, // Stub value
            ChatRooms = new List<ChatRoomResponse>()
        };
    }

    public async Task<bool> DeleteSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                return false;
            }

            // Only allow deletion of sessions that are not active
            if (session.Status == SessionStatus.Active)
            {
                _logger.LogWarning("Cannot delete active session: {SessionId}", sessionId);
                return false;
            }

            await _chatSessionRepository.DeleteAsync(sessionId, cancellationToken);
            // TODO: Add cache removal when cache service is available

            _logger.LogInformation("Session deleted: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session: {SessionId}", sessionId);
            return false;
        }
    }
}