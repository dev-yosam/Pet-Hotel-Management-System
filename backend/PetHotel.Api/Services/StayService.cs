using System.Data;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class StayService(
    PetHotelDbContext dbContext,
    TimeProvider timeProvider,
    IStayChargeCalculator chargeCalculator) : IStayService
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

        var stayInfo = await dbContext.Stays
            .AsNoTracking()
            .Where(stay => stay.Id == id)
            .Select(stay => new { stay.RoomId, stay.PetId })
            .SingleOrDefaultAsync(cancellationToken);

        if (stayInfo is null)
        {
            throw new StayException(
                StayError.NotFound,
                "Stay not found.");
        }

        // Keep the same pet, room, stay lock order as booking.
        await dbContext.Pets
            .FromSqlInterpolated($"""SELECT * FROM "Pets" WHERE "Id" = {stayInfo.PetId} FOR UPDATE""")
            .AsNoTracking().ToListAsync(cancellationToken);

        var rooms = await dbContext.Rooms
            .FromSqlInterpolated(
                $"""SELECT * FROM "Rooms" WHERE "Id" = {stayInfo.RoomId} FOR UPDATE""")
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

        // A pet must check out before checking into another room.
        if (await dbContext.Stays.AnyAsync(other => other.Id != stay.Id &&
            other.PetId == stay.PetId && other.Status == StayStatus.CheckedIn &&
            other.ActualCheckOut == null, cancellationToken))
            throw new StayException(StayError.Conflict, "This pet is already checked in.");

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

    public async Task<StayResponse> CheckOutAsync(
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

        var stayInfo = await dbContext.Stays
            .AsNoTracking()
            .Where(stay => stay.Id == id)
            .Select(stay => new { stay.RoomId, stay.PetId })
            .SingleOrDefaultAsync(cancellationToken);

        if (stayInfo is null)
        {
            throw new StayException(
                StayError.NotFound,
                "Stay not found.");
        }

        // Use the same lock order so requests do not block each other in reverse order.
        await dbContext.Pets
            .FromSqlInterpolated($"""SELECT * FROM "Pets" WHERE "Id" = {stayInfo.PetId} FOR UPDATE""")
            .AsNoTracking().ToListAsync(cancellationToken);

        var rooms = await dbContext.Rooms
            .FromSqlInterpolated(
                $"""SELECT * FROM "Rooms" WHERE "Id" = {stayInfo.RoomId} FOR UPDATE""")
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

        await dbContext.Entry(stay).ReloadAsync(cancellationToken);

        if (stay.RoomId != room.Id)
        {
            throw new StayException(
                StayError.Conflict,
                "The assigned room has changed. Refresh and try again.");
        }

        if (stay.Status != StayStatus.CheckedIn ||
            stay.ActualCheckIn is null ||
            stay.ActualCheckOut is not null ||
            stay.TotalCharge is not null)
        {
            throw new StayException(
                StayError.Conflict,
                "Only checked-in stays without a settlement can check out.");
        }

        var now = timeProvider.GetUtcNow();

        if (now < stay.ActualCheckIn.Value)
        {
            throw new StayException(
                StayError.Conflict,
                "Check-out cannot be before actual check-in.");
        }

        var totalCharge = chargeCalculator.CalculateCharge(
            room.RoomType,
            stay.ScheduledCheckIn,
            stay.ScheduledCheckOut);

        if (totalCharge < 0m || totalCharge > 99999999.99m)
        {
            throw new StayException(
                StayError.InvalidInput,
                "The calculated charge is outside the supported range.");
        }

        stay.TotalCharge = totalCharge;
        stay.ActualCheckOut = now;
        stay.Status = StayStatus.CheckedOut;

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
            TotalCharge = stay.TotalCharge,
            Status = stay.Status.ToString()
        };
    }
}
