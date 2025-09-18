using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using WaglBackend.Infrastructure.Templates.Authorization;

namespace WaglBackend.Infrastructure.Templates.Controllers.Chat;

[Authorize(Policy = ChatAuthorizationPolicies.ChatAccess)]
[ApiController]
[Route("api/v{version:apiVersion}/chat/sessions")]
public class ChatSessionController : BaseApiController
{
    private readonly IChatSessionService _chatSessionService;

    public ChatSessionController(
        IChatSessionService chatSessionService,
        ILogger<ChatSessionController> logger) : base(logger)
    {
        _chatSessionService = chatSessionService;
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatSessionResponse>> CreateSession(
        [FromBody] ChatSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = string.IsNullOrEmpty(GetUserId()) ? null : UserId.From(GetUserId());
            var session = await _chatSessionService.CreateSessionAsync(request, userId, cancellationToken);

            Logger.LogInformation("Chat session created with ID: {SessionId} by user: {UserId}",
                session.Id, userId?.ToString() ?? "Anonymous");

            return Ok(session);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid request for creating chat session: {Message}", ex.Message);
            return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating chat session");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to create chat session" });
        }
    }

    /// <summary>
    /// Start a scheduled chat session
    /// </summary>
    [HttpPost("{sessionId}/start")]
    [Authorize(Policy = ChatAuthorizationPolicies.SessionParticipant)]
    public async Task<ActionResult<ChatSessionResponse>> StartSession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var session = await _chatSessionService.StartSessionAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Chat session started: {SessionId}", sessionId);

            return Ok(session);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID for starting session: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot start session {SessionId}: {Message}", sessionId, ex.Message);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting chat session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to start chat session" });
        }
    }

    /// <summary>
    /// End an active chat session
    /// </summary>
    [HttpPost("{sessionId}/end")]
    [Authorize(Policy = ChatAuthorizationPolicies.SessionParticipant)]
    public async Task<ActionResult<ChatSessionResponse>> EndSession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var session = await _chatSessionService.EndSessionAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Chat session ended: {SessionId}", sessionId);

            return Ok(session);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID for ending session: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot end session {SessionId}: {Message}", sessionId, ex.Message);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error ending chat session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to end chat session" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific chat session
    /// </summary>
    [HttpGet("{sessionId}")]
    [Authorize(Policy = ChatAuthorizationPolicies.SessionParticipant)]
    public async Task<ActionResult<ChatSessionResponse>> GetSession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var session = await _chatSessionService.GetSessionWithDetailsAsync(sessionIdValue, cancellationToken);

            if (session == null)
            {
                Logger.LogWarning("Chat session not found: {SessionId}", sessionId);
                return NotFound(new { error = "SESSION_NOT_FOUND", message = "Chat session not found" });
            }

            return Ok(session);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving chat session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve chat session" });
        }
    }

    /// <summary>
    /// Get session status and basic information
    /// </summary>
    [HttpGet("{sessionId}/status")]
    [Authorize(Policy = ChatAuthorizationPolicies.SessionParticipant)]
    public async Task<ActionResult<object>> GetSessionStatus(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var session = await _chatSessionService.GetSessionAsync(sessionIdValue, cancellationToken);

            if (session == null)
            {
                Logger.LogWarning("Chat session not found for status check: {SessionId}", sessionId);
                return NotFound(new { error = "SESSION_NOT_FOUND", message = "Chat session not found" });
            }

            var participantCount = await _chatSessionService.GetActiveParticipantCountAsync(sessionIdValue, cancellationToken);

            var statusResponse = new
            {
                SessionId = session.Id,
                Name = session.Name,
                Status = session.Status.ToString(),
                ScheduledStartTime = session.ScheduledStartTime,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                ParticipantCount = participantCount,
                MaxParticipants = session.MaxParticipants,
                CanStart = session.CanStart,
                IsActive = session.IsActive,
                IsExpired = session.IsExpired
            };

            return Ok(statusResponse);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID for status check: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving session status: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve session status" });
        }
    }

    /// <summary>
    /// Get all active chat sessions
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ChatSessionSummaryResponse>>> GetActiveSessions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _chatSessionService.GetActiveSessionsAsync(cancellationToken);

            Logger.LogInformation("Retrieved {Count} active chat sessions", sessions.Count());

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving active chat sessions");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve active sessions" });
        }
    }

    /// <summary>
    /// Get all scheduled chat sessions
    /// </summary>
    [HttpGet("scheduled")]
    public async Task<ActionResult<IEnumerable<ChatSessionSummaryResponse>>> GetScheduledSessions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _chatSessionService.GetScheduledSessionsAsync(cancellationToken);

            Logger.LogInformation("Retrieved {Count} scheduled chat sessions", sessions.Count());

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving scheduled chat sessions");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve scheduled sessions" });
        }
    }

    /// <summary>
    /// Get sessions created by the current user
    /// </summary>
    [HttpGet("my-sessions")]
    public async Task<ActionResult<IEnumerable<ChatSessionSummaryResponse>>> GetMySessions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdString = GetUserId();
            if (string.IsNullOrEmpty(userIdString))
            {
                return BadRequest(new { error = "UNAUTHORIZED", message = "User ID not found in token" });
            }

            var userId = UserId.From(userIdString);
            var sessions = await _chatSessionService.GetSessionsByUserAsync(userId, cancellationToken);

            Logger.LogInformation("Retrieved {Count} sessions for user: {UserId}", sessions.Count(), userId);

            return Ok(sessions);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid user ID in token: {Message}", ex.Message);
            return BadRequest(new { error = "INVALID_USER_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving user sessions");
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve user sessions" });
        }
    }

    /// <summary>
    /// Delete a chat session (only if not started)
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<ActionResult> DeleteSession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            await _chatSessionService.DeleteSessionAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Chat session deleted: {SessionId}", sessionId);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID for deletion: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot delete session {SessionId}: {Message}", sessionId, ex.Message);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting chat session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to delete chat session" });
        }
    }
}