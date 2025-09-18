using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
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
    private readonly ILogger<ChatMessageService> _logger;

    public ChatMessageService(
        IChatMessageRepository chatMessageRepository,
        IParticipantRepository participantRepository,
        ILogger<ChatMessageService> logger)
    {
        _chatMessageRepository = chatMessageRepository;
        _participantRepository = participantRepository;
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
                SentAt = DateTime.UtcNow
            };

            await _chatMessageRepository.AddAsync(message, cancellationToken);

            _logger.LogInformation("Message {MessageId} sent successfully by participant {ParticipantId}",
                message.Id, participantId);

            return new ChatMessageResponse
            {
                Id = message.Id.ToString(),
                RoomId = message.RoomId.Value.ToString(),
                SessionId = message.SessionId.Value.ToString(),
                ParticipantId = message.ParticipantId.ToString(),
                SenderName = participant.DisplayName,
                Content = message.Content,
                SentAt = message.SentAt
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
}