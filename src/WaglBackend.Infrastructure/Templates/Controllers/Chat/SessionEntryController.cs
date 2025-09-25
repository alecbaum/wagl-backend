using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Templates.Controllers.Base;

namespace WaglBackend.Infrastructure.Templates.Controllers.Chat;

/// <summary>
/// Handles anonymous user entry into chat sessions via unique URLs
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/sessionentry")]
[AllowAnonymous]
public class SessionEntryController : BaseApiController
{
    private readonly IInviteManagementService _inviteManagementService;
    private readonly IRoomAllocationService _roomAllocationService;
    private readonly IParticipantTrackingService _participantTrackingService;
    private readonly IChatSessionService _chatSessionService;

    public SessionEntryController(
        IInviteManagementService inviteManagementService,
        IRoomAllocationService roomAllocationService,
        IParticipantTrackingService participantTrackingService,
        IChatSessionService chatSessionService,
        ILogger<SessionEntryController> logger) : base(logger)
    {
        _inviteManagementService = inviteManagementService;
        _roomAllocationService = roomAllocationService;
        _participantTrackingService = participantTrackingService;
        _chatSessionService = chatSessionService;
    }

    /// <summary>
    /// Anonymous entry point for chat sessions via unique URL code
    /// Example: GET /api/v1/sessionentry/enterSession?code=cDfaoG0k3WmEK1WSgChdDc...
    /// </summary>
    [HttpGet("enterSession")]
    public async Task<ActionResult<SessionEntryResponse>> EnterSession(
        [FromQuery] string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inviteToken = InviteToken.From(code);

            // Validate the invite and get session details
            var inviteValidation = await _inviteManagementService.ValidateInviteAsync(inviteToken, cancellationToken);

            if (!inviteValidation.IsValid)
            {
                Logger.LogWarning("Invalid or expired invite code used: {Code}", code);
                return BadRequest(new {
                    error = "INVALID_INVITE",
                    message = "This invite link is invalid or has expired"
                });
            }

            // Get detailed invite information
            var invite = await _inviteManagementService.GetInviteByTokenAsync(inviteToken, cancellationToken);
            if (invite == null)
            {
                return NotFound(new {
                    error = "INVITE_NOT_FOUND",
                    message = "Invite not found"
                });
            }

            // Get session details
            var sessionId = SessionId.From(invite.SessionId);
            var session = await _chatSessionService.GetSessionAsync(sessionId, cancellationToken);

            if (session == null)
            {
                return NotFound(new {
                    error = "SESSION_NOT_FOUND",
                    message = "Chat session not found"
                });
            }

            Logger.LogInformation("User accessing session entry for session: {SessionId} via code: {Code}",
                session.Id, code);

            return Ok(new SessionEntryResponse
            {
                SessionId = session.Id,
                SessionName = session.Name,
                InviteCode = code,
                IsSessionActive = session.IsActive,
                RequiresEmailAndName = true, // Anonymous users must provide email and display name
                MaxParticipants = session.MaxParticipants,
                CurrentParticipants = session.ActiveParticipants,
                EstimatedWaitTime = CalculateEstimatedWaitTime(session),
                SessionStartTime = session.ScheduledStartTime,
                SessionEndTime = session.ScheduledEndTime
            });
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid invite code format: {Code}", code);
            return BadRequest(new {
                error = "INVALID_CODE_FORMAT",
                message = "Invalid invite code format"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing session entry for code: {Code}", code);
            return StatusCode(500, new {
                error = "INTERNAL_ERROR",
                message = "Failed to process session entry"
            });
        }
    }

    /// <summary>
    /// Join session as anonymous user with email and display name
    /// </summary>
    [HttpPost("joinSession")]
    public async Task<ActionResult<AnonymousJoinResponse>> JoinSession(
        [FromBody] AnonymousJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inviteToken = InviteToken.From(request.InviteCode);

            // Validate email format
            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new {
                    error = "INVALID_EMAIL",
                    message = "Please provide a valid email address"
                });
            }

            // Validate display name
            if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length < 2)
            {
                return BadRequest(new {
                    error = "INVALID_DISPLAY_NAME",
                    message = "Display name must be at least 2 characters long"
                });
            }

            // Consume the invite and get room assignment
            var joinResult = await _inviteManagementService.ConsumeInviteAsync(
                inviteToken,
                request.DisplayName,
                null, // No user ID for anonymous users
                cancellationToken);

            if (!joinResult.Success)
            {
                Logger.LogWarning("Failed to join session for user {DisplayName} with code {Code}: {Message}",
                    request.DisplayName, request.InviteCode, joinResult.ErrorMessage);

                return BadRequest(new {
                    error = "JOIN_FAILED",
                    message = joinResult.ErrorMessage ?? "Failed to join session"
                });
            }

            // Update participant with email information
            if (!string.IsNullOrEmpty(joinResult.ParticipantId))
            {
                await UpdateParticipantEmail(Guid.Parse(joinResult.ParticipantId), request.Email, cancellationToken);
            }

            Logger.LogInformation("Anonymous user {DisplayName} ({Email}) joined session via room {RoomId}",
                request.DisplayName, request.Email, joinResult.RoomId);

            return Ok(new AnonymousJoinResponse
            {
                Success = true,
                ParticipantId = joinResult.ParticipantId,
                RoomId = joinResult.RoomId,
                SessionId = joinResult.Participant?.SessionId ?? string.Empty,
                DisplayName = request.DisplayName,
                Email = request.Email,
                SignalRConnectionToken = GenerateSignalRToken(joinResult.ParticipantId),
                Message = "Successfully joined the chat session"
            });
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid request for joining session: {Message}", ex.Message);
            return BadRequest(new {
                error = "VALIDATION_ERROR",
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot join session: {Message}", ex.Message);
            return BadRequest(new {
                error = "INVALID_OPERATION",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error joining session for user {DisplayName} with code {Code}",
                request.DisplayName, request.InviteCode);

            return StatusCode(500, new {
                error = "INTERNAL_ERROR",
                message = "Failed to join session"
            });
        }
    }

    /// <summary>
    /// Get available rooms for a session (for load balancing display)
    /// </summary>
    [HttpGet("session/{sessionId}/rooms/availability")]
    public async Task<ActionResult<RoomAvailabilityResponse>> GetRoomAvailability(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var rooms = await _roomAllocationService.GetAvailableRoomsAsync(sessionIdValue, cancellationToken);

            var availability = rooms.Select(room => new RoomAvailabilityInfo
            {
                RoomId = room.Id,
                CurrentParticipants = room.ParticipantCount,
                MaxParticipants = room.MaxParticipants,
                IsAvailable = room.ParticipantCount < room.MaxParticipants,
                EstimatedWaitTime = CalculateRoomWaitTime(room)
            }).ToList();

            return Ok(new RoomAvailabilityResponse
            {
                SessionId = sessionId,
                Rooms = availability,
                TotalAvailableSpots = availability.Sum(r => r.MaxParticipants - r.CurrentParticipants),
                RecommendedRoomId = GetRecommendedRoom(availability)?.RoomId
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new {
                error = "INVALID_SESSION_ID",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting room availability for session: {SessionId}", sessionId);
            return StatusCode(500, new {
                error = "INTERNAL_ERROR",
                message = "Failed to get room availability"
            });
        }
    }

    #region Private Helper Methods

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task UpdateParticipantEmail(Guid participantId, string email, CancellationToken cancellationToken)
    {
        try
        {
            // This would update the participant record with email information
            // Implementation depends on your participant tracking service
            var participant = await _participantTrackingService.GetByIdAsync(participantId, cancellationToken);
            if (participant != null)
            {
                // Update participant with email - this may require adding email field to participant entity
                Logger.LogInformation("Updated participant {ParticipantId} with email {Email}", participantId, email);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update participant email for {ParticipantId}", participantId);
        }
    }

    private string GenerateSignalRToken(string? participantId)
    {
        // Generate a simple token for SignalR connection
        // In production, this should be a proper JWT or connection token
        return $"signalr_{participantId}_{DateTime.UtcNow.Ticks}";
    }

    private int CalculateEstimatedWaitTime(ChatSessionResponse session)
    {
        // Simple estimation based on current load
        if (session.ActiveParticipants >= session.MaxParticipants)
        {
            return 5; // 5 minutes if full
        }

        var capacityRatio = (double)session.ActiveParticipants / session.MaxParticipants;
        return capacityRatio switch
        {
            > 0.9 => 3, // 3 minutes if 90%+ full
            > 0.7 => 1, // 1 minute if 70%+ full
            _ => 0      // No wait if under 70%
        };
    }

    private int CalculateRoomWaitTime(ChatRoomSummaryResponse room)
    {
        // Simple room-level wait time calculation
        if (room.ParticipantCount >= room.MaxParticipants)
            return int.MaxValue; // Room full

        var capacityRatio = (double)room.ParticipantCount / room.MaxParticipants;
        return capacityRatio > 0.8 ? 2 : 0;
    }

    private RoomAvailabilityInfo? GetRecommendedRoom(List<RoomAvailabilityInfo> rooms)
    {
        // Recommend room with lowest occupancy that's available
        return rooms
            .Where(r => r.IsAvailable)
            .OrderBy(r => r.CurrentParticipants)
            .FirstOrDefault();
    }

    #endregion
}

#region DTOs

public class AnonymousJoinRequest
{
    public string InviteCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class SessionEntryResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public bool IsSessionActive { get; set; }
    public bool RequiresEmailAndName { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public int EstimatedWaitTime { get; set; }
    public DateTime SessionStartTime { get; set; }
    public DateTime SessionEndTime { get; set; }
}

public class AnonymousJoinResponse
{
    public bool Success { get; set; }
    public string? ParticipantId { get; set; }
    public string? RoomId { get; set; }
    public string? SessionId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SignalRConnectionToken { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class RoomAvailabilityResponse
{
    public string SessionId { get; set; } = string.Empty;
    public List<RoomAvailabilityInfo> Rooms { get; set; } = new();
    public int TotalAvailableSpots { get; set; }
    public string? RecommendedRoomId { get; set; }
}

public class RoomAvailabilityInfo
{
    public string RoomId { get; set; } = string.Empty;
    public int CurrentParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsAvailable { get; set; }
    public int EstimatedWaitTime { get; set; }
}

#endregion