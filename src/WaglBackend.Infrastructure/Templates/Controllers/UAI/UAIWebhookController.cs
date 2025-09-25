using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Asp.Versioning;
using WaglBackend.Core.Molecules.DTOs.Request.UAI;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Templates.Controllers.Base;

namespace WaglBackend.Infrastructure.Templates.Controllers.UAI;

/// <summary>
/// TODO: Placeholder controller - UAI doesn't send moderator/bot messages yet
/// Controller for receiving webhook messages from UAI service
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/uai/webhook")]
public class UAIWebhookController : BaseApiController
{
    private readonly IUAIWebhookService _uaiWebhookService;

    public UAIWebhookController(
        IUAIWebhookService uaiWebhookService,
        ILogger<UAIWebhookController> logger) : base(logger)
    {
        _uaiWebhookService = uaiWebhookService;
    }

    /// <summary>
    /// TODO: Placeholder endpoint - UAI doesn't send moderator messages yet
    /// Receive moderator messages from UAI (sends to all rooms in session)
    /// </summary>
    [HttpPost("moderator-message")]
    [AllowAnonymous] // TODO: Add proper webhook authentication
    public async Task<ActionResult> ReceiveModeratorMessage(
        [FromBody] UAIModeratorMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogWarning("Received moderator message from UAI but this feature is not implemented yet. Request: {@Request}", request);

            // TODO: Validate webhook signature
            // var isValid = await _uaiWebhookService.ValidateWebhookAsync(signature, payload, cancellationToken);
            // if (!isValid) return Unauthorized();

            var success = await _uaiWebhookService.ProcessModeratorMessageAsync(request, cancellationToken);

            if (success)
            {
                Logger.LogInformation("Successfully processed moderator message from UAI: {MessageId}", request.MessageId);
                return Ok(new { success = true, message = "Moderator message processed" });
            }

            Logger.LogWarning("Failed to process moderator message from UAI: {MessageId}", request.MessageId);
            return BadRequest(new { success = false, message = "Failed to process moderator message" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing moderator message from UAI");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// TODO: Placeholder endpoint - UAI doesn't send bot messages yet
    /// Receive bot messages from UAI (sends to specific room)
    /// </summary>
    [HttpPost("bot-message")]
    [AllowAnonymous] // TODO: Add proper webhook authentication
    public async Task<ActionResult> ReceiveBotMessage(
        [FromBody] UAIBotMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogWarning("Received bot message from UAI but this feature is not implemented yet. Request: {@Request}", request);

            // TODO: Validate webhook signature
            // var isValid = await _uaiWebhookService.ValidateWebhookAsync(signature, payload, cancellationToken);
            // if (!isValid) return Unauthorized();

            var success = await _uaiWebhookService.ProcessBotMessageAsync(request, cancellationToken);

            if (success)
            {
                Logger.LogInformation("Successfully processed bot message from UAI: {MessageId}", request.MessageId);
                return Ok(new { success = true, message = "Bot message processed" });
            }

            Logger.LogWarning("Failed to process bot message from UAI: {MessageId}", request.MessageId);
            return BadRequest(new { success = false, message = "Failed to process bot message" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing bot message from UAI");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check endpoint for UAI to verify webhook connectivity
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult Health()
    {
        Logger.LogInformation("UAI webhook health check requested");
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "UAI Webhook Controller",
            endpoints = new[]
            {
                "/api/v1/uai/webhook/moderator-message",
                "/api/v1/uai/webhook/bot-message",
                "/api/v1/uai/webhook/health"
            }
        });
    }
}