using Microsoft.Extensions.Logging;
using WaglBackend.Core.Molecules.DTOs.Request.UAI;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

/// <summary>
/// TODO: Placeholder implementation - UAI doesn't send moderator/bot messages yet
/// Service for processing inbound webhook messages from UAI
/// </summary>
public class UAIWebhookService : IUAIWebhookService
{
    private readonly ILogger<UAIWebhookService> _logger;

    public UAIWebhookService(ILogger<UAIWebhookService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessModeratorMessageAsync(UAIModeratorMessageRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ProcessModeratorMessageAsync called but UAI doesn't support moderator messages yet. Request: {@Request}", request);

        // TODO: Implement when UAI supports moderator messages:
        // 1. Find session by UAI session ID
        // 2. Create system moderator participant if not exists
        // 3. Create ChatMessage with MessageType.ModeratorMessage
        // 4. Store message in database
        // 5. Broadcast to all rooms in session via SignalR

        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> ProcessBotMessageAsync(UAIBotMessageRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ProcessBotMessageAsync called but UAI doesn't support bot messages yet. Request: {@Request}", request);

        // TODO: Implement when UAI supports bot messages:
        // 1. Find session by UAI session ID
        // 2. Map UAI room number to our RoomId
        // 3. Create system bot participant if not exists
        // 4. Create ChatMessage with MessageType.BotMessage
        // 5. Store message in database
        // 6. Broadcast to specific room via SignalR

        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> ValidateWebhookAsync(string signature, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ValidateWebhookAsync called but webhook authentication not implemented yet");

        // TODO: Implement proper webhook validation:
        // 1. Verify HMAC signature
        // 2. Check timestamp to prevent replay attacks
        // 3. Validate payload format

        await Task.CompletedTask;
        return true; // For now, accept all requests (dev only)
    }
}