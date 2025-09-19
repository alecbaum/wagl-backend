using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Core.Molecules.DTOs.Response;

namespace WaglBackend.Domain.Organisms.Services;

public interface IParticipantTrackingService
{
    Task<Participant> CreateParticipantAsync(RoomId roomId, SessionId sessionId, string displayName, UserId? userId = null, string? connectionId = null, CancellationToken cancellationToken = default);
    Task<ParticipantResponse?> GetParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<Participant?> GetByIdAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<Participant?> GetParticipantByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParticipantResponse>> GetParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParticipantResponse>> GetActiveParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParticipantResponse>> GetParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParticipantResponse>> GetActiveParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> UpdateConnectionIdAsync(Guid participantId, string? connectionId, CancellationToken cancellationToken = default);
    Task<bool> MarkParticipantAsLeftAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> MarkParticipantAsActiveAsync(Guid participantId, string connectionId, CancellationToken cancellationToken = default);
    Task<bool> IsParticipantActiveAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> IsUserInSessionAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default);
    Task<int> GetActiveParticipantCountAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> GetTotalParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ParticipantResponse>> GetParticipantsByTypeAsync(ParticipantType type, CancellationToken cancellationToken = default);
    Task<int> GetTotalParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
}