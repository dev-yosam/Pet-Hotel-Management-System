namespace PetHotel.Api.DTOs;

public record DashboardSummary(
    DateOnly Date, int CurrentPets, int ActiveRooms, int OccupiedRooms,
    decimal OccupancyPercent, int TodayCheckIns, int TodayCheckOuts,
    int CareTasks, int CompletedCareTasks, int PendingCareTasks, decimal CareCompletionPercent);
