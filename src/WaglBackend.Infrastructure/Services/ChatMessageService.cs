using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

public class ChatMessageService : IChatMessageService
{
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IUAIIntegrationService _uaiIntegrationService;
    private readonly ISystemParticipantService _systemParticipantService;
    private readonly ILogger<ChatMessageService> _logger;

    public ChatMessageService(
        IChatMessageRepository chatMessageRepository,
        IParticipantRepository participantRepository,
        IUAIIntegrationService uaiIntegrationService,
        ISystemParticipantService systemParticipantService,
        ILogger<ChatMessageService> logger)
    {
        _chatMessageRepository = chatMessageRepository;
        _participantRepository = participantRepository;
        _uaiIntegrationService = uaiIntegrationService;
        _systemParticipantService = systemParticipantService;
        _logger = logger;
    }

    public async Task<ChatMessageResponse> SendMessageAsync(ChatMessageRequest request, Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending message from participant {ParticipantId} to room {RoomId}",
                participantId, request.RoomId);

            // Validate message content
            if (!await IsMessageValidAsync(request.Content, cancellationToken))
            {
                throw new ArgumentException("Message content is invalid");
            }

            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);
            if (participant == null)
            {
                throw new ArgumentException($"Participant {participantId} not found");
            }

            if (!participant.IsActive)
            {
                throw new InvalidOperationException("Participant is not active");
            }

            var roomId = RoomId.From(Guid.Parse(request.RoomId));
            if (participant.RoomId.Value != roomId.Value)
            {
                throw new InvalidOperationException("Participant is not in the specified room");
            }

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                SessionId = participant.SessionId,
                ParticipantId = participantId,
                Content = request.Content.Trim(),
                SentAt = DateTime.UtcNow,
                MessageType = MessageType.UserMessage
            };

            await _chatMessageRepository.AddAsync(message, cancellationToken);

            _logger.LogInformation("Message {MessageId} sent successfully by participant {ParticipantId}",
                message.Id, participantId);

            // Send message to UAI in background (don't block user experience)
            _ = Task.Run(async () =>
            {
                try
                {
                    var uaiSessionId = _uaiIntegrationService.GetUAISessionId(participant.SessionId);
                    var uaiRoomNumber = _uaiIntegrationService.GetUAIRoomNumber(participant.RoomId);

                    await _uaiIntegrationService.SendMessageAsync(message, uaiSessionId, uaiRoomNumber, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send message {MessageId} to UAI - continuing without UAI integration", message.Id);
                }
            }, cancellationToken);

            return new ChatMessageResponse
            {
                Id = message.Id.ToString(),
                RoomId = message.RoomId.Value.ToString(),
                SessionId = message.SessionId.Value.ToString(),
                ParticipantId = message.ParticipantId.ToString(),
                SenderName = participant.DisplayName,
                Content = message.Content,
                SentAt = message.SentAt,
                MessageType = message.MessageType,
                ExternalMessageId = message.ExternalMessageId,
                TriggerMessageId = message.TriggerMessageId?.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message from participant {ParticipantId}", participantId);
            throw;
        }
    }

    public async Task<ChatMessageResponse?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _chatMessageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                return null;
            }

            var participant = await _participantRepository.GetByIdAsync(message.ParticipantId, cancellationToken);

            return new ChatMessageResponse
            {
                Id = message.Id.ToString(),
                RoomId = message.RoomId.Value.ToString(),
                SessionId = message.SessionId.Value.ToString(),
                ParticipantId = message.ParticipantId.ToString(),
                SenderName = participant?.DisplayName ?? "Unknown",
                Content = message.Content,
                SentAt = message.SentAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _chatMessageRepository.GetMessagesByRoomAsync(roomId, cancellationToken);
            var result = new List<ChatMessageResponse>();

            foreach (var message in messages)
            {
                var participant = await _participantRepository.GetByIdAsync(message.ParticipantId, cancellationToken);

                result.Add(new ChatMessageResponse
                {
                    Id = message.Id.ToString(),
                    RoomId = message.RoomId.Value.ToString(),
                    SessionId = message.SessionId.Value.ToString(),
                    ParticipantId = message.ParticipantId.ToString(),
                    SenderName = participant?.DisplayName ?? "Unknown",
                    Content = message.Content,
                    SentAt = message.SentAt
                });
            }

            return result.OrderBy(m => m.SentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<MessageHistoryResponse> GetMessageHistoryAsync(RoomId roomId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await _chatMessageRepository.GetMessageCountByRoomAsync(roomId, cancellationToken);
            var messages = await _chatMessageRepository.GetMessagesByRoomPaginatedAsync(roomId, page, pageSize, cancellationToken);

            var messageResponses = new List<ChatMessageResponse>();

            foreach (var message in messages)
            {
                var participant = await _participantRepository.GetByIdAsync(message.ParticipantId, cancellationToken);

                messageResponses.Add(new ChatMessageResponse
                {
                    Id = message.Id.ToString(),
                    RoomId = message.RoomId.Value.ToString(),
                    SessionId = message.SessionId.Value.ToString(),
                    ParticipantId = message.ParticipantId.ToString(),
                    SenderName = participant?.DisplayName ?? "Unknown",
                    Content = message.Content,
                    SentAt = message.SentAt
                });
            }

            return new MessageHistoryResponse
            {
                RoomId = roomId.Value.ToString(),
                Messages = messageResponses.OrderBy(m => m.SentAt).ToList(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalMessages = totalCount,
                HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message history for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetRecentMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _chatMessageRepository.GetRecentMessagesAsync(roomId, count, cancellationToken);
            var result = new List<ChatMessageResponse>();

            foreach (var message in messages)
            {
                var participant = await _participantRepository.GetByIdAsync(message.ParticipantId, cancellationToken);

                result.Add(new ChatMessageResponse
                {
                    Id = message.Id.ToString(),
                    RoomId = message.RoomId.Value.ToString(),
                    SessionId = message.SessionId.Value.ToString(),
                    ParticipantId = message.ParticipantId.ToString(),
                    SenderName = participant?.DisplayName ?? "Unknown",
                    Content = message.Content,
                    SentAt = message.SentAt
                });
            }

            return result.OrderBy(m => m.SentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent messages for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetMessagesAfterAsync(RoomId roomId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _chatMessageRepository.GetMessagesAfterAsync(roomId, timestamp, cancellationToken);
            var result = new List<ChatMessageResponse>();

            foreach (var message in messages)
            {
                var participant = await _participantRepository.GetByIdAsync(message.ParticipantId, cancellationToken);

                result.Add(new ChatMessageResponse
                {
                    Id = message.Id.ToString(),
                    RoomId = message.RoomId.Value.ToString(),
                    SessionId = message.SessionId.Value.ToString(),
                    ParticipantId = message.ParticipantId.ToString(),
                    SenderName = participant?.DisplayName ?? "Unknown",
                    Content = message.Content,
                    SentAt = message.SentAt
                });
            }

            return result.OrderBy(m => m.SentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages after {Timestamp} for room {RoomId}",
                timestamp, roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetMessagesBeforeAsync(RoomId roomId, DateTime timestamp, int count = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _chatMessageRepository.GetMessagesBeforeAsync(roomId, timestamp, count, cancellationToken);
            var result = new List<ChatMessageResponse>();

            foreach (var message in messages)
            {
                var participant = await _participantRepository.GetByIdAsync(message.ParticipantId, cancellationToken);

                result.Add(new ChatMessageResponse
                {
                    Id = message.Id.ToString(),
                    RoomId = message.RoomId.Value.ToString(),
                    SessionId = message.SessionId.Value.ToString(),
                    ParticipantId = message.ParticipantId.ToString(),
                    SenderName = participant?.DisplayName ?? "Unknown",
                    Content = message.Content,
                    SentAt = message.SentAt
                });
            }

            return result.OrderBy(m => m.SentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages before {Timestamp} for room {RoomId}",
                timestamp, roomId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetMessagesByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _chatMessageRepository.GetMessagesByParticipantAsync(participantId, cancellationToken);
            var participant = await _participantRepository.GetByIdAsync(participantId, cancellationToken);

            return messages.Select(message => new ChatMessageResponse
            {
                Id = message.Id.ToString(),
                RoomId = message.RoomId.Value.ToString(),
                SessionId = message.SessionId.Value.ToString(),
                ParticipantId = message.ParticipantId.ToString(),
                SenderName = participant?.DisplayName ?? "Unknown",
                Content = message.Content,
                SentAt = message.SentAt
            }).OrderBy(m => m.SentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages by participant {ParticipantId}", participantId);
            throw;
        }
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId, Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting message {MessageId} by participant {ParticipantId}",
                messageId, participantId);

            var message = await _chatMessageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                _logger.LogWarning("Message {MessageId} not found", messageId);
                return false;
            }

            // Check if participant can delete this message
            if (!await CanDeleteMessageAsync(messageId, participantId, cancellationToken))
            {
                _logger.LogWarning("Participant {ParticipantId} cannot delete message {MessageId}",
                    participantId, messageId);
                return false;
            }

            await _chatMessageRepository.DeleteAsync(messageId, cancellationToken);

            _logger.LogInformation("Successfully deleted message {MessageId}", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<bool> CanDeleteMessageAsync(Guid messageId, Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _chatMessageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                return false;
            }

            // Participants can only delete their own messages
            // In a more complex system, you might have moderators who can delete any message
            return message.ParticipantId == participantId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check delete permissions for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task<int> GetMessageCountByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _chatMessageRepository.GetMessageCountByRoomAsync(roomId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count for room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<int> GetMessageCountByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _chatMessageRepository.GetMessageCountByParticipantAsync(participantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count for participant {ParticipantId}", participantId);
            throw;
        }
    }

    public async Task<bool> DeleteMessagesByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting all messages in room {RoomId}", roomId.Value);

            var deletedCount = await _chatMessageRepository.DeleteByRoomIdAsync(roomId, cancellationToken);

            _logger.LogInformation("Deleted {Count} messages from room {RoomId}", deletedCount, roomId.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete messages from room {RoomId}", roomId.Value);
            throw;
        }
    }

    public async Task<bool> IsMessageValidAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic validation rules
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            // Check message length (max 2000 characters)
            if (content.Trim().Length > 2000)
            {
                return false;
            }

            // Check for only whitespace or special characters
            if (content.Trim().All(char.IsWhiteSpace))
            {
                return false;
            }

            // Additional validation can be added here:
            // - Profanity filtering
            // - Spam detection
            // - Rate limiting per participant
            // - Content moderation

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate message content");
            return false;
        }
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetRoomMessagesAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await GetMessagesByRoomAsync(roomId, cancellationToken);
    }

    public async Task<int> GetRoomMessageCountAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await GetMessageCountByRoomAsync(roomId, cancellationToken);
    }

    public async Task<IEnumerable<ChatMessageResponse>> GetRecentRoomMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default)
    {
        return await GetRecentMessagesAsync(roomId, count, cancellationToken);
    }

    public async Task<double> GetAverageMessageLengthAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _chatMessageRepository.GetByRoomIdAsync(roomId, cancellationToken);
            var activeMessages = messages.Where(m => !m.IsDeleted).ToList();

            if (!activeMessages.Any())
                return 0;

            return activeMessages.Average(m => m.Content.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average message length for room: {RoomId}", roomId);
            return 0;
        }
    }

    // UAI Integration Methods for Inbound Messages
    // TODO: Placeholder methods - UAI doesn't send bot/moderator messages yet

    /// <summary>
    /// Creates a moderator message from UAI (sends to all rooms in session)
    /// TODO: Placeholder - for when UAI sends moderator messages
    /// </summary>
    public async Task<ChatMessageResponse> CreateModeratorMessageAsync(
        SessionId sessionId,
        string content,
        string externalMessageId,
        Guid? triggerMessageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating moderator message from UAI for session {SessionId}", sessionId);

            // Get or create system moderator participant
            var moderatorParticipant = await _systemParticipantService.GetOrCreateSystemModeratorAsync(sessionId, cancellationToken);

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                RoomId = RoomId.From(Guid.Empty), // Special value for session-wide messages
                SessionId = sessionId,
                ParticipantId = moderatorParticipant.Id,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow,
                MessageType = MessageType.ModeratorMessage,
                ExternalMessageId = externalMessageId,
                TriggerMessageId = triggerMessageId
            };

            await _chatMessageRepository.AddAsync(message, cancellationToken);

            _logger.LogInformation("Moderator message {MessageId} created from UAI external message {ExternalMessageId}",
                message.Id, externalMessageId);

            return new ChatMessageResponse
            {
                Id = message.Id.ToString(),
                RoomId = message.RoomId.Value.ToString(),
                SessionId = message.SessionId.Value.ToString(),
                ParticipantId = message.ParticipantId.ToString(),
                SenderName = moderatorParticipant.DisplayName,
                Content = message.Content,
                SentAt = message.SentAt,
                MessageType = message.MessageType,
                ExternalMessageId = message.ExternalMessageId,
                TriggerMessageId = message.TriggerMessageId?.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create moderator message from UAI for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Creates a bot message from UAI (sends to specific room)
    /// TODO: Placeholder - for when UAI sends bot messages
    /// </summary>
    public async Task<ChatMessageResponse> CreateBotMessageAsync(
        RoomId roomId,
        SessionId sessionId,
        string content,
        string externalMessageId,
        Guid? triggerMessageId = null,
        string botName = "UAI Bot",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating bot message from UAI for room {RoomId}", roomId);

            // Get or create bot participant for this room
            var botParticipant = await _systemParticipantService.GetOrCreateBotParticipantAsync(
                sessionId, roomId, botName, cancellationToken);

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                SessionId = sessionId,
                ParticipantId = botParticipant.Id,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow,
                MessageType = MessageType.BotMessage,
                ExternalMessageId = externalMessageId,
                TriggerMessageId = triggerMessageId
            };

            await _chatMessageRepository.AddAsync(message, cancellationToken);

            _logger.LogInformation("Bot message {MessageId} created from UAI external message {ExternalMessageId} for room {RoomId}",
                message.Id, externalMessageId, roomId);

            return new ChatMessageResponse
            {
                Id = message.Id.ToString(),
                RoomId = message.RoomId.Value.ToString(),
                SessionId = message.SessionId.Value.ToString(),
                ParticipantId = message.ParticipantId.ToString(),
                SenderName = botParticipant.DisplayName,
                Content = message.Content,
                SentAt = message.SentAt,
                MessageType = message.MessageType,
                ExternalMessageId = message.ExternalMessageId,
                TriggerMessageId = message.TriggerMessageId?.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bot message from UAI for room {RoomId}", roomId);
            throw;
        }
    }

    /// <summary>
    /// Checks if a message with the given external ID already exists (deduplication)
    /// </summary>
    public async Task<bool> MessageExistsAsync(string externalMessageId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(externalMessageId))
                return false;

            var existingMessage = await _chatMessageRepository.GetByExternalMessageIdAsync(externalMessageId, cancellationToken);
            return existingMessage != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if message exists with external ID {ExternalMessageId}", externalMessageId);
            throw;
        }
    }

    /// <summary>
    /// Gets all rooms in a session for broadcasting moderator messages
    /// </summary>
    public async Task<IEnumerable<RoomId>> GetSessionRoomsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all active participants in the session to find which rooms are active
            var participants = await _participantRepository.GetActiveParticipantsBySessionAsync(sessionId, cancellationToken);

            // Return unique room IDs from active participants
            return participants
                .Where(p => p.RoomId.Value != Guid.Empty) // Exclude session-wide participants
                .Select(p => p.RoomId)
                .Distinct();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session rooms for session {SessionId}", sessionId);
            throw;
        }
    }

}