namespace WaglBackend.Core.Molecules.DTOs.Response;

public class SessionStatisticsResponse
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public int TotalParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class ParticipantStatisticsResponse
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalParticipants { get; set; }
    public int ActiveParticipants { get; set; }
    public int InactiveParticipants { get; set; }
    public int RegisteredParticipants { get; set; }
    public int GuestParticipants { get; set; }
    public double AverageSessionDuration { get; set; }
    public DateTime? FirstParticipantJoined { get; set; }
    public DateTime? LastParticipantLeft { get; set; }
}

public class RoomStatisticsResponse
{
    public string SessionId { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int FullRooms { get; set; }
    public int EmptyRooms { get; set; }
    public double AverageParticipantsPerRoom { get; set; }
    public int MaxParticipantsInAnyRoom { get; set; }
    public int MinParticipantsInAnyRoom { get; set; }
}