using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class DashboardService(PetHotelDbContext db, TimeProvider timeProvider) : IDashboardService
{
    private static readonly TimeZoneInfo HotelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), HotelTimeZone).Date;
        // Convert each midnight separately because daylight saving can change the day length.
        var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(today, HotelTimeZone));
        var end = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(today.AddDays(1), HotelTimeZone));
        var currentStays = db.Stays.Where(stay => stay.Status == StayStatus.CheckedIn
            && stay.ActualCheckIn != null && stay.ActualCheckOut == null);
        var currentPets = await currentStays.Select(stay => stay.PetId).Distinct().CountAsync(cancellationToken);
        var activeRooms = await db.Rooms.CountAsync(room => room.IsActive, cancellationToken);
        // Only active rooms are included in the occupancy percentage.
        var occupiedRooms = await currentStays.Where(stay => stay.Room.IsActive)
            .Select(stay => stay.RoomId).Distinct().CountAsync(cancellationToken);
        // Count actual arrivals and departures, not planned bookings.
        var checkIns = await db.Stays.CountAsync(stay => stay.ActualCheckIn >= start
            && stay.ActualCheckIn < end, cancellationToken);
        var checkOuts = await db.Stays.CountAsync(stay => stay.ActualCheckOut >= start
            && stay.ActualCheckOut < end, cancellationToken);
        var todayCare = db.CareRecords.Where(record => record.ScheduledAt >= start && record.ScheduledAt < end);
        var careTasks = await todayCare.CountAsync(cancellationToken);
        var completed = await todayCare.CountAsync(record => record.IsCompleted, cancellationToken);
        // Start with all rooms so empty rooms also appear on the dashboard.
        var rooms = await db.Rooms.AsNoTracking().OrderBy(room => room.RoomNumber)
            .Select(room => new DashboardRoom(room.Id, room.RoomNumber, room.RoomType, room.IsActive,
                room.Stays.Where(stay => stay.Status == StayStatus.CheckedIn
                    && stay.ActualCheckIn != null && stay.ActualCheckOut == null)
                    .OrderByDescending(stay => stay.ActualCheckIn).ThenByDescending(stay => stay.Id)
                    .Select(stay => stay.Pet.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return new DashboardSummary(DateOnly.FromDateTime(today), currentPets, activeRooms, occupiedRooms,
            Percentage(occupiedRooms, activeRooms), checkIns, checkOuts, careTasks, completed,
            careTasks - completed, Percentage(completed, careTasks)) { Rooms = rooms };
    }

    private static decimal Percentage(int count, int total) =>
        total == 0 ? 0 : Math.Round(count * 100m / total, 1);
}
