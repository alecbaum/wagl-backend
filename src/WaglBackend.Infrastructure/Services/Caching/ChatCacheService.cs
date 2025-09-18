using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Services.Caching;

namespace WaglBackend.Infrastructure.Services.Caching;

public class ChatCacheService : IChatCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ChatCacheService> _logger;

    private static readonly DistributedCacheEntryOptions DefaultOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    private static readonly DistributedCacheEntryOptions SessionOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(15)
    };

    private static readonly DistributedCacheEntryOptions ParticipantOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    public ChatCacheService(IDistributedCache cache, ILogger<ChatCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region Session Caching

    public async Task<ChatSessionResponse?> GetSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSessionKey(sessionId);
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
                return null;

            var session = JsonSerializer.Deserialize<ChatSessionResponse>(cachedData);
            _logger.LogDebug("Session retrieved from cache: {SessionId}", sessionId);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session from cache: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task SetSessionAsync(SessionId sessionId, ChatSessionResponse session, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSessionKey(sessionId);
            var serializedData = JsonSerializer.Serialize(session);

            await _cache.SetStringAsync(key, serializedData, SessionOptions, cancellationToken);
            _logger.LogDebug("Session cached: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching session: {SessionId}", sessionId);
        }
    }

    public async Task RemoveSessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSessionKey(sessionId);
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Session removed from cache: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing session from cache: {SessionId}", sessionId);
        }
    }

    #endregion

    #region Room Caching

    public async Task<ChatRoomResponse?> GetRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetRoomKey(roomId);
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
                return null;

            var room = JsonSerializer.Deserialize<ChatRoomResponse>(cachedData);
            _logger.LogDebug("Room retrieved from cache: {RoomId}", roomId);
            return room;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room from cache: {RoomId}", roomId);
            return null;
        }
    }

    public async Task SetRoomAsync(RoomId roomId, ChatRoomResponse room, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetRoomKey(roomId);
            var serializedData = JsonSerializer.Serialize(room);

            await _cache.SetStringAsync(key, serializedData, DefaultOptions, cancellationToken);
            _logger.LogDebug("Room cached: {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching room: {RoomId}", roomId);
        }
    }

    public async Task RemoveRoomAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetRoomKey(roomId);
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Room removed from cache: {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing room from cache: {RoomId}", roomId);
        }
    }

    #endregion

    #region Participant Caching

    public async Task<List<ParticipantResponse>?> GetRoomParticipantsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetRoomParticipantsKey(roomId);
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
                return null;

            var participants = JsonSerializer.Deserialize<List<ParticipantResponse>>(cachedData);
            _logger.LogDebug("Room participants retrieved from cache: {RoomId}", roomId);
            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room participants from cache: {RoomId}", roomId);
            return null;
        }
    }

    public async Task SetRoomParticipantsAsync(RoomId roomId, List<ParticipantResponse> participants, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetRoomParticipantsKey(roomId);
            var serializedData = JsonSerializer.Serialize(participants);

            await _cache.SetStringAsync(key, serializedData, ParticipantOptions, cancellationToken);
            _logger.LogDebug("Room participants cached: {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching room participants: {RoomId}", roomId);
        }
    }

    public async Task RemoveRoomParticipantsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetRoomParticipantsKey(roomId);
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Room participants removed from cache: {RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing room participants from cache: {RoomId}", roomId);
        }
    }

    #endregion

    #region Connection Tracking

    public async Task<string?> GetConnectionRoomAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetConnectionKey(connectionId);
            var roomId = await _cache.GetStringAsync(key, cancellationToken);

            if (!string.IsNullOrEmpty(roomId))
                _logger.LogDebug("Connection room retrieved from cache: {ConnectionId} -> {RoomId}", connectionId, roomId);

            return roomId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving connection room from cache: {ConnectionId}", connectionId);
            return null;
        }
    }

    public async Task SetConnectionRoomAsync(string connectionId, RoomId roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetConnectionKey(connectionId);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2), // Longer for active connections
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };

            await _cache.SetStringAsync(key, roomId.Value.ToString(), options, cancellationToken);
            _logger.LogDebug("Connection room cached: {ConnectionId} -> {RoomId}", connectionId, roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching connection room: {ConnectionId}", connectionId);
        }
    }

    public async Task RemoveConnectionRoomAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetConnectionKey(connectionId);
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Connection room removed from cache: {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection room from cache: {ConnectionId}", connectionId);
        }
    }

    #endregion

    #region Invite Token Caching

    public async Task<SessionInviteResponse?> GetInviteAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetInviteKey(token);
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
                return null;

            var invite = JsonSerializer.Deserialize<SessionInviteResponse>(cachedData);
            _logger.LogDebug("Invite retrieved from cache: {Token}", token.Value[..8] + "...");
            return invite;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invite from cache: {Token}", token.Value[..8] + "...");
            return null;
        }
    }

    public async Task SetInviteAsync(InviteToken token, SessionInviteResponse invite, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetInviteKey(token);
            var serializedData = JsonSerializer.Serialize(invite);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24), // Match invite expiration
                SlidingExpiration = TimeSpan.FromHours(1)
            };

            await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
            _logger.LogDebug("Invite cached: {Token}", token.Value[..8] + "...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching invite: {Token}", token.Value[..8] + "...");
        }
    }

    public async Task RemoveInviteAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetInviteKey(token);
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Invite removed from cache: {Token}", token.Value[..8] + "...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing invite from cache: {Token}", token.Value[..8] + "...");
        }
    }

    #endregion

    #region Bulk Operations

    public async Task InvalidateSessionCacheAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = new List<Task>
            {
                RemoveSessionAsync(sessionId, cancellationToken)
            };

            // Also remove related room caches - would need to implement pattern matching or store room lists
            _logger.LogDebug("Invalidated session cache: {SessionId}", sessionId);

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating session cache: {SessionId}", sessionId);
        }
    }

    #endregion

    #region Private Helper Methods

    private static string GetSessionKey(SessionId sessionId) => $"wagl:chat:session:{sessionId.Value}";
    private static string GetRoomKey(RoomId roomId) => $"wagl:chat:room:{roomId.Value}";
    private static string GetRoomParticipantsKey(RoomId roomId) => $"wagl:chat:room:{roomId.Value}:participants";
    private static string GetConnectionKey(string connectionId) => $"wagl:chat:connection:{connectionId}";
    private static string GetInviteKey(InviteToken token) => $"wagl:chat:invite:{token.Value}";

    #endregion
}