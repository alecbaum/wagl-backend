using WaglBackend.Core.Molecules.DTOs.Request.UAI;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

/// <summary>
/// TODO: Placeholder - UAI doesn't send moderator/bot messages yet
/// Service for processing inbound webhook messages from UAI
/// </summary>
public interface IUAIWebhookService
{
    /// <summary>
    /// Process a moderator message from UAI (sends to all rooms in session)
    /// TODO: Implement when UAI supports sending moderator messages
    /// </summary>
    Task<bool> ProcessModeratorMessageAsync(UAIModeratorMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a bot message from UAI (sends to specific room)
    /// TODO: Implement when UAI supports sending bot messages
    /// </summary>
    Task<bool> ProcessBotMessageAsync(UAIBotMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the webhook request is authentic and from UAI
    /// TODO: Implement proper webhook authentication
    /// </summary>
    Task<bool> ValidateWebhookAsync(string signature, string payload, CancellationToken cancellationToken = default);
}