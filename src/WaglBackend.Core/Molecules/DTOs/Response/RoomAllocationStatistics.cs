namespace WaglBackend.Core.Molecules.DTOs.Response;

public class RoomAllocationStatistics
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public double AverageOccupancyRate { get; set; }
    public int TotalParticipants { get; set; }
    public int MaxCapacity { get; set; }
    public Dictionary<string, int> RoomOccupancyDistribution { get; set; } = new();
}