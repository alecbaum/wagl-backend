using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using WaglBackend.Infrastructure.Templates.Authorization;

namespace WaglBackend.Infrastructure.Templates.Controllers.Chat;

[Authorize(Policy = ChatAuthorizationPolicies.ChatAccess)]
[ApiController]
[Route("api/v{version:apiVersion}/chat/invites")]
public class SessionInviteController : BaseApiController
{
    private readonly IInviteManagementService _inviteManagementService;

    public SessionInviteController(
        IInviteManagementService inviteManagementService,
        ILogger<SessionInviteController> logger) : base(logger)
    {
        _inviteManagementService = inviteManagementService;
    }

    /// <summary>
    /// Generate a new invite link for a chat session
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ChatAuthorizationPolicies.SessionParticipant)]
    public async Task<ActionResult<SessionInviteResponse>> GenerateInvite(
        [FromBody] SessionInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = SessionId.From(request.SessionId);
            var invite = await _inviteManagementService.GenerateInviteAsync(request,
                cancellationToken);

            Logger.LogInformation("Invite generated for session {SessionId} with token {Token}",
                request.SessionId, invite.Token);

            return Ok(invite);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid request for generating invite: {Message}", ex.Message);
            return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot generate invite: {Message}", ex.Message);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating invite for session: {SessionId}", request.SessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to generate invite" });
        }
    }

    /// <summary>
    /// Generate multiple invite links for a chat session
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkSessionInviteResponse>> GenerateBulkInvites(
        [FromBody] BulkSessionInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionId = SessionId.From(request.SessionId);
            var result = await _inviteManagementService.GenerateBulkInvitesAsync(request, cancellationToken);

            Logger.LogInformation("Generated {SuccessCount} of {TotalCount} invites for session {SessionId}",
                result.SuccessfulInvites, request.Recipients.Count, request.SessionId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid request for generating bulk invites: {Message}", ex.Message);
            return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating bulk invites for session: {SessionId}", request.SessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to generate bulk invites" });
        }
    }

    /// <summary>
    /// Validate an invite token (without consuming it)
    /// </summary>
    [HttpGet("{token}/validate")]
    [AllowAnonymous]
    public async Task<ActionResult<InviteValidationResponse>> ValidateInvite(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inviteToken = InviteToken.From(token);
            var validationResult = await _inviteManagementService.ValidateInviteAsync(inviteToken, cancellationToken);

            Logger.LogInformation("Invite validation for token {Token}: {IsValid}",
                token, validationResult.IsValid);

            return Ok(validationResult);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid invite token format: {Token}", token);
            return BadRequest(new { error = "INVALID_TOKEN", message = "Invalid invite token format" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating invite token: {Token}", token);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to validate invite" });
        }
    }

    /// <summary>
    /// Get detailed information about an invite (without consuming it)
    /// </summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionInviteResponse>> GetInviteDetails(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inviteToken = InviteToken.From(token);
            var invite = await _inviteManagementService.GetInviteByTokenAsync(inviteToken, cancellationToken);

            if (invite == null)
            {
                Logger.LogWarning("Invite not found for token: {Token}", token);
                return NotFound(new { error = "INVITE_NOT_FOUND", message = "Invite token not found" });
            }

            Logger.LogInformation("Retrieved invite details for token: {Token}", token);

            return Ok(invite);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid invite token format: {Token}", token);
            return BadRequest(new { error = "INVALID_TOKEN", message = "Invalid invite token format" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving invite details for token: {Token}", token);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve invite details" });
        }
    }

    /// <summary>
    /// Consume an invite token and join the session
    /// </summary>
    [HttpPost("{token}/consume")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomJoinResponse>> ConsumeInvite(
        string token,
        [FromBody] ConsumeInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inviteToken = InviteToken.From(token);
            var userId = string.IsNullOrEmpty(GetUserId()) ? null : UserId.From(GetUserId());

            var joinResult = await _inviteManagementService.ConsumeInviteAsync(
                inviteToken,
                request.DisplayName,
                userId,
                cancellationToken);

            Logger.LogInformation("Invite consumed for token {Token} by user {DisplayName}",
                token, request.DisplayName);

            return Ok(joinResult);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid request for consuming invite: {Message}", ex.Message);
            return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot consume invite {Token}: {Message}", token, ex.Message);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error consuming invite token: {Token}", token);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to consume invite" });
        }
    }

    /// <summary>
    /// Get all invites for a specific session
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<IEnumerable<SessionInviteResponse>>> GetSessionInvites(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var invites = await _inviteManagementService.GetInvitesBySessionAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Retrieved {Count} invites for session: {SessionId}",
                invites.Count(), sessionId);

            return Ok(invites);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving invites for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve session invites" });
        }
    }

    /// <summary>
    /// Get all active (unused) invites for a specific session
    /// </summary>
    [HttpGet("session/{sessionId}/active")]
    public async Task<ActionResult<IEnumerable<SessionInviteResponse>>> GetActiveSessionInvites(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var invites = await _inviteManagementService.GetActiveInvitesBySessionAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Retrieved {Count} active invites for session: {SessionId}",
                invites.Count(), sessionId);

            return Ok(invites);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving active invites for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve active session invites" });
        }
    }

    /// <summary>
    /// Expire/deactivate an invite
    /// </summary>
    [HttpDelete("{token}")]
    public async Task<ActionResult> ExpireInvite(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inviteToken = InviteToken.From(token);
            await _inviteManagementService.ExpireInviteAsync(inviteToken, cancellationToken);

            Logger.LogInformation("Invite expired: {Token}", token);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid invite token for expiration: {Token}", token);
            return BadRequest(new { error = "INVALID_TOKEN", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning("Cannot expire invite {Token}: {Message}", token, ex.Message);
            return BadRequest(new { error = "INVALID_OPERATION", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error expiring invite token: {Token}", token);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to expire invite" });
        }
    }

    /// <summary>
    /// Get invite statistics for a session
    /// </summary>
    [HttpGet("session/{sessionId}/statistics")]
    public async Task<ActionResult<object>> GetInviteStatistics(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var statistics = await _inviteManagementService.GetInviteStatisticsAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Retrieved invite statistics for session: {SessionId}", sessionId);

            return Ok(statistics);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID for statistics: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving invite statistics for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve invite statistics" });
        }
    }
}

/// <summary>
/// Request model for consuming an invite
/// </summary>
public class ConsumeInviteRequest
{
    public string DisplayName { get; set; } = string.Empty;
}