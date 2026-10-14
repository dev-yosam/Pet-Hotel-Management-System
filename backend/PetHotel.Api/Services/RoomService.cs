using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class RoomService(PetHotelDbContext dbContext) : IRoomService
{
    public async Task<List<RoomResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .OrderBy(room => room.RoomNumber)
            .ThenBy(room => room.Id)
            .Select(room => new RoomResponse
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                IsActive = room.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RoomResponse>> GetAvailableAsync(
        DateTimeOffset checkIn,
        DateTimeOffset checkOut,
        CancellationToken cancellationToken = default)
    {
        if (checkOut <= checkIn)
        {
            throw new ValidationException(
                "Check-out must be after check-in.");
        }

        var start = checkIn.ToUniversalTime();
        var end = checkOut.ToUniversalTime();

        return await dbContext.Rooms
            .AsNoTracking()
            .Where(room => room.IsActive)
            .Where(room => !room.Stays.Any(stay =>
                (stay.Status == StayStatus.Booked ||
                 stay.Status == StayStatus.CheckedIn) &&
                stay.ScheduledCheckIn < end &&
                stay.ScheduledCheckOut > start))
            .OrderBy(room => room.RoomNumber)
            .ThenBy(room => room.Id)
            .Select(room => new RoomResponse
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                IsActive = room.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}