using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Request;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

public interface IInviteManagementService
{
    Task<SessionInviteResponse> CreateInviteAsync(SessionInviteRequest request, CancellationToken cancellationToken = default);
    Task<BulkSessionInviteResponse> CreateBulkInvitesAsync(BulkSessionInviteRequest request, CancellationToken cancellationToken = default);
    Task<InviteValidationResponse> ValidateInviteAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<RoomJoinResponse> ConsumeInviteAsync(InviteToken token, string displayName, UserId? userId = null, CancellationToken cancellationToken = default);
    Task<SessionInviteResponse?> GetInviteAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInviteResponse>> GetInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInviteResponse>> GetActiveInvitesAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInviteResponse>> GetConsumedInvitesAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> ExpireInviteAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<bool> DeleteExpiredInvitesAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateInviteUrlAsync(InviteToken token, string baseUrl, CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(InviteToken token, CancellationToken cancellationToken = default);
    Task<int> GetActiveInviteCountAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionInviteResponse>> GetActiveInvitesBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<InviteStatisticsResponse> GetInviteStatisticsAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<SessionInviteResponse> GenerateInviteAsync(SessionInviteRequest request, CancellationToken cancellationToken = default);
    Task<BulkSessionInviteResponse> GenerateBulkInvitesAsync(BulkSessionInviteRequest request, CancellationToken cancellationToken = default);
    Task<SessionInviteResponse?> GetInviteByTokenAsync(InviteToken token, CancellationToken cancellationToken = default);
}