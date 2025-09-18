using Microsoft.Extensions.Logging;
using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;
using WaglBackend.Domain.Organisms.Repositories;
using WaglBackend.Domain.Organisms.Services;

namespace WaglBackend.Infrastructure.Services;

public class InviteManagementService : IInviteManagementService
{
    private readonly ISessionInviteRepository _sessionInviteRepository;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly IRoomAllocationService _roomAllocationService;
    private readonly IParticipantTrackingService _participantTrackingService;
    private readonly ILogger<InviteManagementService> _logger;

    public InviteManagementService(
        ISessionInviteRepository sessionInviteRepository,
        IChatSessionRepository chatSessionRepository,
        IRoomAllocationService roomAllocationService,
        IParticipantTrackingService participantTrackingService,
        ILogger<InviteManagementService> logger)
    {
        _sessionInviteRepository = sessionInviteRepository;
        _chatSessionRepository = chatSessionRepository;
        _roomAllocationService = roomAllocationService;
        _participantTrackingService = participantTrackingService;
        _logger = logger;
    }

    public async Task<SessionInviteResponse> CreateInviteAsync(SessionInviteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating invite for session {SessionId}", request.SessionId);

            var sessionId = SessionId.From(Guid.Parse(request.SessionId));
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);

            if (session == null)
            {
                throw new ArgumentException($"Session {request.SessionId} not found");
            }

            if (session.Status != SessionStatus.Scheduled)
            {
                throw new InvalidOperationException($"Cannot create invites for session with status {session.Status}");
            }

            var invite = new SessionInvite
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Token = InviteToken.Create(),
                InviteeEmail = request.InviteeEmail,
                InviteeName = request.InviteeName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes),
                IsConsumed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _sessionInviteRepository.AddAsync(invite, cancellationToken);

            _logger.LogInformation("Created invite {InviteId} for session {SessionId}",
                invite.Id, sessionId.Value);

            return MapToResponse(invite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invite for session {SessionId}", request.SessionId);
            throw;
        }
    }

    public async Task<BulkSessionInviteResponse> CreateBulkInvitesAsync(BulkSessionInviteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating {Count} invites for session {SessionId}",
                request.Recipients.Count, request.SessionId);

            var sessionId = SessionId.From(Guid.Parse(request.SessionId));
            var session = await _chatSessionRepository.GetByIdAsync(sessionId, cancellationToken);

            if (session == null)
            {
                throw new ArgumentException($"Session {request.SessionId} not found");
            }

            if (session.Status != SessionStatus.Scheduled)
            {
                throw new InvalidOperationException($"Cannot create invites for session with status {session.Status}");
            }

            var invites = new List<SessionInvite>();
            var expiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes);

            foreach (var recipient in request.Recipients)
            {
                var invite = new SessionInvite
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    Token = InviteToken.Create(),
                    InviteeEmail = recipient.Email,
                    InviteeName = recipient.Name,
                    ExpiresAt = expiresAt,
                    IsConsumed = false,
                    CreatedAt = DateTime.UtcNow
                };

                invites.Add(invite);
                await _sessionInviteRepository.AddAsync(invite, cancellationToken);
            }

            _logger.LogInformation("Created {Count} invites for session {SessionId}",
                invites.Count, sessionId.Value);

            return new BulkSessionInviteResponse
            {
                SessionId = request.SessionId,
                TotalInvites = invites.Count,
                SuccessfulInvites = invites.Count,
                FailedInvites = 0,
                Invites = invites.Select(MapToResponse).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bulk invites for session {SessionId}", request.SessionId);
            throw;
        }
    }

    public async Task<InviteValidationResponse> ValidateInviteAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating invite token {Token}", token.Value);

            var invite = await _sessionInviteRepository.GetByTokenAsync(token, cancellationToken);

            if (invite == null)
            {
                return new InviteValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid invite token"
                };
            }

            var session = await _chatSessionRepository.GetByIdAsync(invite.SessionId, cancellationToken);

            if (session == null)
            {
                return new InviteValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Session not found"
                };
            }

            // Check if invite has expired
            if (invite.IsExpired)
            {
                return new InviteValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invite has expired"
                };
            }

            // Check if invite is already consumed
            if (invite.IsConsumed)
            {
                return new InviteValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invite has already been used"
                };
            }

            // Check if session is in a valid state for joining
            if (session.Status != SessionStatus.Scheduled && session.Status != SessionStatus.Active)
            {
                return new InviteValidationResponse
                {
                    IsValid = false,
                    ErrorMessage = $"Session is {session.Status.ToString().ToLower()} and cannot be joined"
                };
            }

            return new InviteValidationResponse
            {
                IsValid = true,
                SessionId = session.Id.Value.ToString(),
                SessionName = session.Name,
                ScheduledStartTime = session.ScheduledStartTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate invite token {Token}", token.Value);
            throw;
        }
    }

    public async Task<RoomJoinResponse> ConsumeInviteAsync(InviteToken token, string displayName, UserId? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Consuming invite token {Token} for user {DisplayName}",
                token.Value, displayName);

            // First validate the invite
            var validation = await ValidateInviteAsync(token, cancellationToken);
            if (!validation.IsValid)
            {
                return new RoomJoinResponse
                {
                    Success = false,
                    ErrorMessage = validation.ErrorMessage
                };
            }

            var invite = await _sessionInviteRepository.GetByTokenAsync(token, cancellationToken);
            var sessionId = SessionId.From(Guid.Parse(validation.SessionId!));

            // Allocate participant to room (this handles both finding/creating room and adding participant)
            var roomJoinResult = await _roomAllocationService.AllocateParticipantToRoomAsync(
                sessionId, displayName, userId, cancellationToken);

            if (!roomJoinResult.Success)
            {
                return roomJoinResult;
            }

            // Consume the invite
            invite!.Consume(userId, displayName);
            await _sessionInviteRepository.UpdateAsync(invite, cancellationToken);

            _logger.LogInformation("Successfully consumed invite {InviteId} for participant {ParticipantId}",
                invite.Id, roomJoinResult.ParticipantId);

            return roomJoinResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consume invite token {Token}", token.Value);
            return new RoomJoinResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while processing your invite"
            };
        }
    }

    public async Task<SessionInviteResponse?> GetInviteAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var invite = await _sessionInviteRepository.GetByTokenAsync(token, cancellationToken);
            return invite != null ? MapToResponse(invite) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invite by token {Token}", token.Value);
            throw;
        }
    }

    public async Task<IEnumerable<SessionInviteResponse>> GetInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invites = await _sessionInviteRepository.GetInvitesBySessionAsync(sessionId, cancellationToken);
            return invites.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invites for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<SessionInviteResponse>> GetActiveInvitesAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invites = await _sessionInviteRepository.GetInvitesBySessionAsync(sessionId, cancellationToken);
            var activeInvites = invites.Where(i => i.IsValid);
            return activeInvites.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active invites for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<IEnumerable<SessionInviteResponse>> GetConsumedInvitesAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invites = await _sessionInviteRepository.GetInvitesBySessionAsync(sessionId, cancellationToken);
            var consumedInvites = invites.Where(i => i.IsConsumed);
            return consumedInvites.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consumed invites for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    public async Task<bool> ExpireInviteAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Expiring invite token {Token}", token.Value);

            var invite = await _sessionInviteRepository.GetByTokenAsync(token, cancellationToken);
            if (invite == null)
            {
                _logger.LogWarning("Invite token {Token} not found", token.Value);
                return false;
            }

            // Mark as expired by setting ExpiresAt to past
            invite.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            await _sessionInviteRepository.UpdateAsync(invite, cancellationToken);

            _logger.LogInformation("Successfully expired invite token {Token}", token.Value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire invite token {Token}", token.Value);
            throw;
        }
    }

    public async Task<bool> DeleteExpiredInvitesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting expired invites");

            var allInvites = await _sessionInviteRepository.GetAllAsync(cancellationToken);
            var expiredInvites = allInvites.Where(i => i.IsExpired);

            var deletedCount = 0;
            foreach (var invite in expiredInvites)
            {
                await _sessionInviteRepository.DeleteAsync(invite.Id, cancellationToken);
                deletedCount++;
            }

            _logger.LogInformation("Deleted {Count} expired invites", deletedCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expired invites");
            throw;
        }
    }

    public async Task<string> GenerateInviteUrlAsync(InviteToken token, string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var invite = await _sessionInviteRepository.GetByTokenAsync(token, cancellationToken);
            if (invite == null)
            {
                throw new ArgumentException($"Invite token {token.Value} not found");
            }

            var cleanBaseUrl = baseUrl.TrimEnd('/');
            return $"{cleanBaseUrl}/join/{token.Value}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invite URL for token {Token}", token.Value);
            throw;
        }
    }

    public async Task<bool> IsTokenValidAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = await ValidateInviteAsync(token, cancellationToken);
            return validation.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check token validity for {Token}", token.Value);
            throw;
        }
    }

    public async Task<int> GetActiveInviteCountAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invites = await _sessionInviteRepository.GetInvitesBySessionAsync(sessionId, cancellationToken);
            return invites.Count(i => i.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active invite count for session {SessionId}", sessionId.Value);
            throw;
        }
    }

    private static SessionInviteResponse MapToResponse(SessionInvite invite)
    {
        return new SessionInviteResponse
        {
            Id = invite.Id.ToString(),
            SessionId = invite.SessionId.Value.ToString(),
            Token = invite.Token.Value,
            InviteeEmail = invite.InviteeEmail,
            InviteeName = invite.InviteeName,
            ExpiresAt = invite.ExpiresAt,
            IsConsumed = invite.IsConsumed,
            ConsumedAt = invite.ConsumedAt,
            ConsumedByUserId = invite.ConsumedByUserId?.Value.ToString(),
            ConsumedByName = invite.ConsumedByName,
            IsExpired = invite.IsExpired,
            IsValid = invite.IsValid,
            CanBeUsed = invite.IsValid, // Simplified - just check if valid
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<IEnumerable<SessionInviteResponse>> GetActiveInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await GetActiveInvitesAsync(sessionId, cancellationToken);
    }

    public async Task<InviteStatisticsResponse> GetInviteStatisticsAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invites = await _sessionInviteRepository.GetBySessionIdAsync(sessionId, cancellationToken);
            var inviteList = invites.ToList();

            var totalInvites = inviteList.Count;
            var activeInvites = inviteList.Count(i => i.IsValid);
            var consumedInvites = inviteList.Count(i => i.IsConsumed);
            var expiredInvites = inviteList.Count(i => i.IsExpired);

            var conversionRate = totalInvites > 0 ? (double)consumedInvites / totalInvites * 100 : 0;

            return new InviteStatisticsResponse
            {
                SessionId = sessionId.Value.ToString(),
                TotalInvites = totalInvites,
                ActiveInvites = activeInvites,
                ConsumedInvites = consumedInvites,
                ExpiredInvites = expiredInvites,
                ConversionRate = conversionRate,
                LastInviteCreated = inviteList.OrderByDescending(i => i.CreatedAt).FirstOrDefault()?.CreatedAt,
                LastInviteConsumed = inviteList.Where(i => i.ConsumedAt.HasValue).OrderByDescending(i => i.ConsumedAt).FirstOrDefault()?.ConsumedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invite statistics for session: {SessionId}", sessionId);
            return new InviteStatisticsResponse
            {
                SessionId = sessionId.Value.ToString()
            };
        }
    }

    public async Task<SessionInviteResponse> GenerateInviteAsync(SessionInviteRequest request, CancellationToken cancellationToken = default)
    {
        return await CreateInviteAsync(request, cancellationToken);
    }

    public async Task<BulkSessionInviteResponse> GenerateBulkInvitesAsync(BulkSessionInviteRequest request, CancellationToken cancellationToken = default)
    {
        return await CreateBulkInvitesAsync(request, cancellationToken);
    }

    public async Task<SessionInviteResponse?> GetInviteByTokenAsync(InviteToken token, CancellationToken cancellationToken = default)
    {
        return await GetInviteAsync(token, cancellationToken);
    }
}