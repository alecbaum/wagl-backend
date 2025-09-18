using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Templates.Hubs;

public class ChatHub : Hub
{
    private readonly IInviteManagementService _inviteManagementService;
    private readonly IRoomAllocationService _roomAllocationService;
    private readonly IParticipantTrackingService _participantTrackingService;
    private readonly IChatMessageService _chatMessageService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IInviteManagementService inviteManagementService,
        IRoomAllocationService roomAllocationService,
        IParticipantTrackingService participantTrackingService,
        IChatMessageService chatMessageService,
        ILogger<ChatHub> logger)
    {
        _inviteManagementService = inviteManagementService;
        _roomAllocationService = roomAllocationService;
        _participantTrackingService = participantTrackingService;
        _chatMessageService = chatMessageService;
        _logger = logger;
    }

    public async Task<RoomJoinResponse> JoinRoomWithToken(string token, string displayName)
    {
        try
        {
            _logger.LogInformation("Participant {DisplayName} attempting to join room with token {Token}", displayName, token);

            var inviteToken = InviteToken.From(token);
            var userId = GetCurrentUserId();

            var joinResult = await _inviteManagementService.ConsumeInviteAsync(inviteToken, displayName, userId);

            if (joinResult.Success && !string.IsNullOrEmpty(joinResult.RoomId))
            {
                // Update participant connection ID
                if (!string.IsNullOrEmpty(joinResult.ParticipantId))
                {
                    await _participantTrackingService.UpdateConnectionIdAsync(
                        Guid.Parse(joinResult.ParticipantId),
                        Context.ConnectionId);
                }

                // Join SignalR group for the room
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{joinResult.RoomId}");

                // Notify others in the room about new participant
                await Clients.Group($"Room_{joinResult.RoomId}")
                    .SendAsync("ParticipantJoined", new
                    {
                        ParticipantId = joinResult.ParticipantId,
                        DisplayName = displayName,
                        JoinedAt = DateTime.UtcNow
                    });

                _logger.LogInformation("Participant {DisplayName} successfully joined room {RoomId}", displayName, joinResult.RoomId);
            }

            return joinResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while participant {DisplayName} was joining room with token {Token}", displayName, token);
            return new RoomJoinResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while joining the room. Please try again."
            };
        }
    }

    public async Task<ChatMessageResponse?> SendMessage(string roomId, string content)
    {
        try
        {
            var participant = await _participantTrackingService.GetParticipantByConnectionIdAsync(Context.ConnectionId);
            if (participant == null)
            {
                _logger.LogWarning("Message send attempt from unknown connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "You are not connected to any room.");
                return null;
            }

            var request = new ChatMessageRequest
            {
                RoomId = roomId,
                Content = content
            };

            var message = await _chatMessageService.SendMessageAsync(request, participant.Id);

            // Broadcast message to all participants in the room
            await Clients.Group($"Room_{roomId}").SendAsync("MessageReceived", message);

            _logger.LogInformation("Message sent by participant {ParticipantId} in room {RoomId}", participant.Id, roomId);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending message in room {RoomId}", roomId);
            await Clients.Caller.SendAsync("Error", "Failed to send message. Please try again.");
            return null;
        }
    }

    public async Task LeaveRoom()
    {
        try
        {
            var participant = await _participantTrackingService.GetParticipantByConnectionIdAsync(Context.ConnectionId);
            if (participant == null)
            {
                _logger.LogWarning("Leave room attempt from unknown connection {ConnectionId}", Context.ConnectionId);
                return;
            }

            var roomId = participant.RoomId.ToString();

            // Mark participant as left
            await _participantTrackingService.MarkParticipantAsLeftAsync(participant.Id);

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{roomId}");

            // Notify others in the room about participant leaving
            await Clients.Group($"Room_{roomId}")
                .SendAsync("ParticipantLeft", new
                {
                    ParticipantId = participant.Id,
                    DisplayName = participant.DisplayName,
                    LeftAt = DateTime.UtcNow
                });

            _logger.LogInformation("Participant {ParticipantId} ({DisplayName}) left room {RoomId}",
                participant.Id, participant.DisplayName, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while participant was leaving room");
        }
    }

    public async Task RequestRoomInfo()
    {
        try
        {
            var participant = await _participantTrackingService.GetParticipantByConnectionIdAsync(Context.ConnectionId);
            if (participant == null)
            {
                await Clients.Caller.SendAsync("Error", "You are not connected to any room.");
                return;
            }

            var roomDetails = await _roomAllocationService.GetRoomDetailsAsync(participant.RoomId);
            await Clients.Caller.SendAsync("RoomInfo", roomDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while requesting room info");
            await Clients.Caller.SendAsync("Error", "Failed to get room information.");
        }
    }

    public async Task RequestMessageHistory(int count = 50)
    {
        try
        {
            var participant = await _participantTrackingService.GetParticipantByConnectionIdAsync(Context.ConnectionId);
            if (participant == null)
            {
                await Clients.Caller.SendAsync("Error", "You are not connected to any room.");
                return;
            }

            var messages = await _chatMessageService.GetRecentMessagesAsync(participant.RoomId, count);
            await Clients.Caller.SendAsync("MessageHistory", messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while requesting message history");
            await Clients.Caller.SendAsync("Error", "Failed to get message history.");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var participant = await _participantTrackingService.GetParticipantByConnectionIdAsync(Context.ConnectionId);
            if (participant != null)
            {
                var roomId = participant.RoomId.ToString();

                // Mark participant as left
                await _participantTrackingService.MarkParticipantAsLeftAsync(participant.Id);

                // Notify others in the room about disconnection
                await Clients.Group($"Room_{roomId}")
                    .SendAsync("ParticipantDisconnected", new
                    {
                        ParticipantId = participant.Id,
                        DisplayName = participant.DisplayName,
                        DisconnectedAt = DateTime.UtcNow
                    });

                _logger.LogInformation("Participant {ParticipantId} ({DisplayName}) disconnected from room {RoomId}",
                    participant.Id, participant.DisplayName, roomId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during client disconnection");
        }

        if (exception != null)
        {
            _logger.LogWarning(exception, "Client {ConnectionId} disconnected with exception", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private UserId? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub") ?? Context.User?.FindFirst("userId");
        if (userIdClaim?.Value != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return UserId.From(userId);
        }
        return null;
    }
}