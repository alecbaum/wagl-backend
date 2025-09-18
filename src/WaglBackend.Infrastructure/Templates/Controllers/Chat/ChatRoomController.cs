using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services;
using WaglBackend.Infrastructure.Templates.Controllers.Base;
using WaglBackend.Infrastructure.Templates.Authorization;

namespace WaglBackend.Infrastructure.Templates.Controllers.Chat;

[Authorize(Policy = ChatAuthorizationPolicies.ChatAccess)]
[ApiController]
[Route("api/v{version:apiVersion}/chat/rooms")]
public class ChatRoomController : BaseApiController
{
    private readonly IRoomAllocationService _roomAllocationService;
    private readonly IParticipantTrackingService _participantTrackingService;
    private readonly IChatMessageService _chatMessageService;

    public ChatRoomController(
        IRoomAllocationService roomAllocationService,
        IParticipantTrackingService participantTrackingService,
        IChatMessageService chatMessageService,
        ILogger<ChatRoomController> logger) : base(logger)
    {
        _roomAllocationService = roomAllocationService;
        _participantTrackingService = participantTrackingService;
        _chatMessageService = chatMessageService;
    }

    /// <summary>
    /// Get all rooms for a specific session
    /// </summary>
    [HttpGet("session/{sessionId}")]
    [Authorize(Policy = ChatAuthorizationPolicies.SessionParticipant)]
    public async Task<ActionResult<IEnumerable<ChatRoomSummaryResponse>>> GetRoomsBySession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var rooms = await _roomAllocationService.GetAvailableRoomsAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Retrieved {Count} rooms for session: {SessionId}",
                rooms.Count(), sessionId);

            return Ok(rooms);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving rooms for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve session rooms" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific room
    /// </summary>
    [HttpGet("{roomId}")]
    [Authorize(Policy = ChatAuthorizationPolicies.RoomParticipant)]
    public async Task<ActionResult<ChatRoomResponse>> GetRoom(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roomIdValue = RoomId.From(roomId);
            var room = await _roomAllocationService.GetRoomWithDetailsAsync(roomIdValue, cancellationToken);

            if (room == null)
            {
                Logger.LogWarning("Room not found: {RoomId}", roomId);
                return NotFound(new { error = "ROOM_NOT_FOUND", message = "Chat room not found" });
            }

            Logger.LogInformation("Retrieved room details: {RoomId}", roomId);

            return Ok(room);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid room ID: {RoomId}", roomId);
            return BadRequest(new { error = "INVALID_ROOM_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving room: {RoomId}", roomId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve room details" });
        }
    }

    /// <summary>
    /// Get all participants in a specific room
    /// </summary>
    [HttpGet("{roomId}/participants")]
    [Authorize(Policy = ChatAuthorizationPolicies.RoomParticipant)]
    public async Task<ActionResult<IEnumerable<ParticipantResponse>>> GetRoomParticipants(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roomIdValue = RoomId.From(roomId);
            var participants = await _participantTrackingService.GetActiveParticipantsByRoomAsync(roomIdValue, cancellationToken);

            Logger.LogInformation("Retrieved {Count} participants for room: {RoomId}",
                participants.Count(), roomId);

            return Ok(participants);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid room ID for participants: {RoomId}", roomId);
            return BadRequest(new { error = "INVALID_ROOM_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving room participants: {RoomId}", roomId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve room participants" });
        }
    }

    /// <summary>
    /// Get chat messages for a specific room with pagination
    /// </summary>
    [HttpGet("{roomId}/messages")]
    public async Task<ActionResult<object>> GetRoomMessages(
        string roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roomIdValue = RoomId.From(roomId);

            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var messageHistory = await _chatMessageService.GetMessageHistoryAsync(roomIdValue, page, pageSize, cancellationToken);

            var response = messageHistory;

            Logger.LogInformation("Retrieved {Count} messages for room {RoomId} (page {Page})",
                messageHistory.Messages.Count, roomId, page);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid room ID for messages: {RoomId}", roomId);
            return BadRequest(new { error = "INVALID_ROOM_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving room messages: {RoomId}", roomId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve room messages" });
        }
    }

    /// <summary>
    /// Get recent messages for a specific room
    /// </summary>
    [HttpGet("{roomId}/messages/recent")]
    public async Task<ActionResult<IEnumerable<ChatMessageResponse>>> GetRecentRoomMessages(
        string roomId,
        [FromQuery] int count = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roomIdValue = RoomId.From(roomId);

            // Validate count parameter
            if (count < 1 || count > 100) count = 20;

            var messages = await _chatMessageService.GetRecentRoomMessagesAsync(roomIdValue, count, cancellationToken);

            Logger.LogInformation("Retrieved {Count} recent messages for room: {RoomId}", messages.Count(), roomId);

            return Ok(messages);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid room ID for recent messages: {RoomId}", roomId);
            return BadRequest(new { error = "INVALID_ROOM_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving recent room messages: {RoomId}", roomId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve recent messages" });
        }
    }

    /// <summary>
    /// Get room statistics and analytics
    /// </summary>
    [HttpGet("{roomId}/statistics")]
    public async Task<ActionResult<object>> GetRoomStatistics(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roomIdValue = RoomId.From(roomId);

            var activeParticipants = await _participantTrackingService.GetActiveParticipantsByRoomAsync(roomIdValue, cancellationToken);
            var totalParticipants = await _participantTrackingService.GetTotalParticipantsByRoomAsync(roomIdValue, cancellationToken);
            var messageCount = await _chatMessageService.GetRoomMessageCountAsync(roomIdValue, cancellationToken);
            var averageMessageLength = await _chatMessageService.GetAverageMessageLengthAsync(roomIdValue, cancellationToken);

            var statistics = new
            {
                RoomId = roomId,
                ActiveParticipants = activeParticipants.Count(),
                TotalParticipants = totalParticipants,
                MessageCount = messageCount,
                AverageMessageLength = averageMessageLength,
                Participants = activeParticipants.Select(p => new
                {
                    p.Id,
                    p.DisplayName,
                    p.Type,
                    p.JoinedAt,
                    p.IsActive,
                    p.IsConnected
                })
            };

            Logger.LogInformation("Retrieved statistics for room: {RoomId}", roomId);

            return Ok(statistics);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid room ID for statistics: {RoomId}", roomId);
            return BadRequest(new { error = "INVALID_ROOM_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving room statistics: {RoomId}", roomId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve room statistics" });
        }
    }

    /// <summary>
    /// Check if a room can accommodate new participants
    /// </summary>
    [HttpGet("{roomId}/availability")]
    public async Task<ActionResult<object>> CheckRoomAvailability(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roomIdValue = RoomId.From(roomId);
            var canJoin = await _roomAllocationService.CanJoinRoomAsync(roomIdValue, cancellationToken);
            var availableSlots = await _roomAllocationService.GetAvailableSlotsAsync(roomIdValue, cancellationToken);

            var availability = new
            {
                RoomId = roomId,
                CanJoin = canJoin,
                AvailableSlots = availableSlots,
                IsFull = !canJoin
            };

            Logger.LogInformation("Checked availability for room {RoomId}: CanJoin={CanJoin}, AvailableSlots={AvailableSlots}",
                roomId, canJoin, availableSlots);

            return Ok(availability);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid room ID for availability check: {RoomId}", roomId);
            return BadRequest(new { error = "INVALID_ROOM_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking room availability: {RoomId}", roomId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to check room availability" });
        }
    }

    /// <summary>
    /// Get all available rooms for a session with their current status
    /// </summary>
    [HttpGet("session/{sessionId}/available")]
    public async Task<ActionResult<IEnumerable<ChatRoomSummaryResponse>>> GetAvailableRooms(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionIdValue = SessionId.From(sessionId);
            var availableRooms = await _roomAllocationService.GetAvailableRoomsAsync(sessionIdValue, cancellationToken);

            Logger.LogInformation("Retrieved {Count} available rooms for session: {SessionId}",
                availableRooms.Count(), sessionId);

            return Ok(availableRooms);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning("Invalid session ID for available rooms: {SessionId}", sessionId);
            return BadRequest(new { error = "INVALID_SESSION_ID", message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving available rooms for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = "Failed to retrieve available rooms" });
        }
    }
}