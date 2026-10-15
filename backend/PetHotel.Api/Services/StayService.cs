using System.Data;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class StayService(
    PetHotelDbContext dbContext,
    TimeProvider timeProvider) : IStayService
{
    public async Task<StayResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var stay = await dbContext.Stays
            .AsNoTracking()
            .SingleOrDefaultAsync(
                stay => stay.Id == id,
                cancellationToken);

        return stay is null ? null : ToResponse(stay);
    }

    public async Task<StayResponse> CheckInAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new StayException(
                StayError.InvalidInput,
                "Stay ID must be greater than zero.");
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var roomId = await dbContext.Stays
            .AsNoTracking()
            .Where(stay => stay.Id == id)
            .Select(stay => (int?)stay.RoomId)
            .SingleOrDefaultAsync(cancellationToken);

        if (roomId is null)
        {
            throw new StayException(
                StayError.NotFound,
                "Stay not found.");
        }

        // Lock the room before the stay, using the booking lock order.
        var rooms = await dbContext.Rooms
            .FromSqlInterpolated(
                $"""SELECT * FROM "Rooms" WHERE "Id" = {roomId.Value} FOR UPDATE""")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var room = rooms.SingleOrDefault();

        if (room is null)
        {
            throw new StayException(
                StayError.NotFound,
                "Room not found.");
        }

        var stays = await dbContext.Stays
            .FromSqlInterpolated(
                $"""SELECT * FROM "Stays" WHERE "Id" = {id} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        var stay = stays.SingleOrDefault();

        if (stay is null)
        {
            throw new StayException(
                StayError.NotFound,
                "Stay not found.");
        }

        // Refresh any entity already tracked by this context.
        await dbContext.Entry(stay).ReloadAsync(cancellationToken);

        if (stay.RoomId != room.Id)
        {
            throw new StayException(
                StayError.Conflict,
                "The assigned room has changed. Refresh and try again.");
        }

        if (stay.Status != StayStatus.Booked ||
            stay.ActualCheckIn is not null ||
            stay.ActualCheckOut is not null)
        {
            throw new StayException(
                StayError.Conflict,
                "Only booked stays without actual stay times can check in.");
        }

        if (!room.IsActive)
        {
            throw new StayException(
                StayError.Conflict,
                "The assigned room is inactive.");
        }

        var now = timeProvider.GetUtcNow();

        if (now < stay.ScheduledCheckIn ||
            now >= stay.ScheduledCheckOut)
        {
            throw new StayException(
                StayError.Conflict,
                "Check-in is only allowed during the booked period.");
        }

        var roomOccupied = await dbContext.Stays.AnyAsync(
            other =>
                other.Id != stay.Id &&
                other.RoomId == stay.RoomId &&
                other.Status == StayStatus.CheckedIn &&
                other.ActualCheckOut == null,
            cancellationToken);

        if (roomOccupied)
        {
            throw new StayException(
                StayError.Conflict,
                "The assigned room still has a checked-in pet.");
        }

        stay.Status = StayStatus.CheckedIn;
        stay.ActualCheckIn = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(stay).ReloadAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ToResponse(stay);
    }

    private static StayResponse ToResponse(Stay stay)
    {
        return new StayResponse
        {
            Id = stay.Id,
            PetId = stay.PetId,
            RoomId = stay.RoomId,
            ScheduledCheckIn = stay.ScheduledCheckIn,
            ScheduledCheckOut = stay.ScheduledCheckOut,
            ActualCheckIn = stay.ActualCheckIn,
            ActualCheckOut = stay.ActualCheckOut,
            Status = stay.Status.ToString()
        };
    }
}