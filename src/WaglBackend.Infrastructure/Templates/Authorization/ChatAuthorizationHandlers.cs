using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Templates.Authorization;

public class SessionParticipantHandler : AuthorizationHandler<SessionParticipantRequirement>
{
    private readonly IParticipantTrackingService _participantTrackingService;
    private readonly IChatSessionService _chatSessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SessionParticipantHandler> _logger;

    public SessionParticipantHandler(
        IParticipantTrackingService participantTrackingService,
        IChatSessionService chatSessionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SessionParticipantHandler> logger)
    {
        _participantTrackingService = participantTrackingService;
        _chatSessionService = chatSessionService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SessionParticipantRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        var sessionIdString = httpContext.Request.RouteValues["sessionId"]?.ToString();
        if (string.IsNullOrEmpty(sessionIdString))
        {
            // Check query parameters as fallback
            sessionIdString = httpContext.Request.Query["sessionId"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(sessionIdString))
        {
            _logger.LogWarning("No session ID found in request for session participant authorization");
            context.Fail();
            return;
        }

        try
        {
            var sessionId = SessionId.From(sessionIdString);
            var userId = GetUserId(context.User);

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            var userIdValue = UserId.From(userId);

            // Check if user is a participant in the session
            var isParticipant = await _participantTrackingService.IsUserInSessionAsync(userIdValue, sessionId);
            if (isParticipant)
            {
                context.Succeed(requirement);
                return;
            }

            // Check if user is the session creator
            var session = await _chatSessionService.GetSessionAsync(sessionId);
            if (session?.CreatedBy != null && session.CreatedBy.Equals(userId))
            {
                context.Succeed(requirement);
                return;
            }

            _logger.LogWarning("User {UserId} is not a participant in session {SessionId}",
                userId, sessionIdString);
            context.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session participant authorization for session {SessionId}",
                sessionIdString);
            context.Fail();
        }
    }

    private static string? GetUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value ??
               user.FindFirst("user_id")?.Value;
    }
}

public class RoomParticipantHandler : AuthorizationHandler<RoomParticipantRequirement>
{
    private readonly IParticipantTrackingService _participantTrackingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RoomParticipantHandler> _logger;

    public RoomParticipantHandler(
        IParticipantTrackingService participantTrackingService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RoomParticipantHandler> logger)
    {
        _participantTrackingService = participantTrackingService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoomParticipantRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            context.Fail();
            return;
        }

        var roomIdString = httpContext.Request.RouteValues["roomId"]?.ToString();
        if (string.IsNullOrEmpty(roomIdString))
        {
            // Check query parameters as fallback
            roomIdString = httpContext.Request.Query["roomId"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(roomIdString))
        {
            _logger.LogWarning("No room ID found in request for room participant authorization");
            context.Fail();
            return;
        }

        try
        {
            var roomId = RoomId.From(roomIdString);
            var userId = GetUserId(context.User);

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return;
            }

            var userIdValue = UserId.From(userId);

            // Check if user is an active participant in the room
            var participants = await _participantTrackingService.GetActiveParticipantsByRoomAsync(roomId);
            var isParticipant = participants.Any(p => p.UserId?.Equals(userIdValue) == true);

            if (isParticipant)
            {
                context.Succeed(requirement);
                return;
            }

            _logger.LogWarning("User {UserId} is not a participant in room {RoomId}",
                userId, roomIdString);
            context.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking room participant authorization for room {RoomId}",
                roomIdString);
            context.Fail();
        }
    }

    private static string? GetUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value ??
               user.FindFirst("user_id")?.Value;
    }
}