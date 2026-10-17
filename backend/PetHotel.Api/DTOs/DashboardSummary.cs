namespace PetHotel.Api.DTOs;

public record DashboardSummary(
    DateOnly Date, int CurrentPets, int ActiveRooms, int OccupiedRooms,
    decimal OccupancyPercent, int TodayCheckIns, int TodayCheckOuts,
    int CareTasks, int CompletedCareTasks, int PendingCareTasks, decimal CareCompletionPercent)
{
    public List<DashboardRoom> Rooms { get; init; } = [];
}

public record DashboardRoom(int Id, string RoomNumber, string RoomType, bool IsActive, string? PetName);
