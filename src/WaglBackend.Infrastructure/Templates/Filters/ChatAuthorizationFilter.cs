using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Templates.Filters;

public class ChatAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<ChatAuthorizationFilter> _logger;
    private readonly IChatSessionService _chatSessionService;
    private readonly IParticipantTrackingService _participantTrackingService;

    public ChatAuthorizationFilter(
        ILogger<ChatAuthorizationFilter> logger,
        IChatSessionService chatSessionService,
        IParticipantTrackingService participantTrackingService)
    {
        _logger = logger;
        _chatSessionService = chatSessionService;
        _participantTrackingService = participantTrackingService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip authorization for endpoints that allow anonymous access
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em is AllowAnonymousAttribute))
        {
            return;
        }

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Extract route parameters
        var sessionId = context.RouteData.Values["sessionId"]?.ToString();
        var roomId = context.RouteData.Values["roomId"]?.ToString();

        try
        {
            // Check session access
            if (!string.IsNullOrEmpty(sessionId))
            {
                var hasSessionAccess = await CheckSessionAccessAsync(user, sessionId);
                if (!hasSessionAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to session {SessionId}",
                        GetUserId(user), sessionId);
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // Check room access
            if (!string.IsNullOrEmpty(roomId))
            {
                var hasRoomAccess = await CheckRoomAccessAsync(user, roomId);
                if (!hasRoomAccess)
                {
                    _logger.LogWarning("User {UserId} denied access to room {RoomId}",
                        GetUserId(user), roomId);
                    context.Result = new ForbidResult();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat authorization for user {UserId}", GetUserId(user));
            context.Result = new StatusCodeResult(500);
        }
    }

    private async Task<bool> CheckSessionAccessAsync(System.Security.Claims.ClaimsPrincipal user, string sessionIdString)
    {
        try
        {
            var sessionId = SessionId.From(sessionIdString);
            var session = await _chatSessionService.GetSessionAsync(sessionId);

            if (session == null)
                return false;

            var userId = GetUserId(user);

            // Session creators always have access
            if (session.CreatedBy != null && session.CreatedBy.Equals(userId))
                return true;

            // Check if user is a participant in the session
            if (!string.IsNullOrEmpty(userId))
            {
                var userIdValue = UserId.From(userId);
                var isParticipant = await _participantTrackingService.IsUserInSessionAsync(userIdValue, sessionId);
                if (isParticipant)
                    return true;
            }

            // Public sessions allow access to authenticated users
            return session.IsPublic;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private async Task<bool> CheckRoomAccessAsync(System.Security.Claims.ClaimsPrincipal user, string roomIdString)
    {
        try
        {
            var roomId = RoomId.From(roomIdString);
            var userId = GetUserId(user);

            if (string.IsNullOrEmpty(userId))
                return false;

            var userIdValue = UserId.From(userId);

            // Check if user is an active participant in the room
            var participants = await _participantTrackingService.GetActiveParticipantsByRoomAsync(roomId);
            return participants.Any(p => p.UserId?.Equals(userIdValue) == true);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string? GetUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value ??
               user.FindFirst("user_id")?.Value;
    }
}

/// <summary>
/// Attribute to require chat session access
/// </summary>
public class RequireSessionAccessAttribute : Attribute, IFilterMetadata
{
}

/// <summary>
/// Attribute to require chat room access
/// </summary>
public class RequireRoomAccessAttribute : Attribute, IFilterMetadata
{
}