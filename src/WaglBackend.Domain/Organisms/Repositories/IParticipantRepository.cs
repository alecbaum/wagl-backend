using WaglBackend.Core.Atoms.Entities;
using WaglBackend.Core.Atoms.Enums;
using WaglBackend.Core.Atoms.ValueObjects;
using WaglBackend.Domain.Organisms.Repositories.Base;

namespace WaglBackend.Domain.Organisms.Repositories;

public interface IParticipantRepository : IRepository<Participant>
{
    Task<IEnumerable<Participant>> GetParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetActiveParticipantsByRoomAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetActiveParticipantsBySessionAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<Participant?> GetParticipantByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<Participant?> GetParticipantByUserIdAsync(UserId userId, RoomId roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetParticipantsByTypeAsync(ParticipantType type, CancellationToken cancellationToken = default);
    Task<bool> UpdateConnectionIdAsync(Guid participantId, string? connectionId, CancellationToken cancellationToken = default);
    Task<bool> MarkParticipantAsLeftAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<int> GetActiveParticipantCountAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<int> GetTotalParticipantCountAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task<bool> IsUserInSessionAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetByUserIdAndSessionIdAsync(UserId userId, SessionId sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetInactiveParticipantsAsync(DateTime inactiveThreshold, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default);

    // System participant methods for UAI integration
    Task<Participant?> GetBySessionAndTypeAsync(SessionId sessionId, ParticipantType participantType, CancellationToken cancellationToken = default);
    Task<Participant?> GetByRoomAndTypeAsync(RoomId roomId, ParticipantType participantType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Participant>> GetBySessionAndTypesAsync(SessionId sessionId, IEnumerable<ParticipantType> participantTypes, CancellationToken cancellationToken = default);
}