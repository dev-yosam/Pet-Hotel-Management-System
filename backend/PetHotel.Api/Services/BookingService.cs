using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class BookingService(PetHotelDbContext dbContext) : IBookingService
{
    public async Task<BookingResponse?> GetByIdAsync(
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

    public async Task<BookingResponse> CreateAsync(
        BookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true))
        {
            throw new BookingException(
                BookingError.InvalidInput,
                string.Join(" ", validationResults.Select(
                    result => result.ErrorMessage)));
        }

        var checkIn = request.ScheduledCheckIn!.Value.ToUniversalTime();
        var checkOut = request.ScheduledCheckOut!.Value.ToUniversalTime();

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // All booking creation must lock the room before checking conflicts.
        var rooms = await dbContext.Rooms
            .FromSqlInterpolated(
                $"""SELECT * FROM "Rooms" WHERE "Id" = {request.RoomId} FOR UPDATE""")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var room = rooms.SingleOrDefault();

        if (room is null)
        {
            throw new BookingException(
                BookingError.NotFound,
                "Room not found.");
        }

        if (!room.IsActive)
        {
            throw new BookingException(
                BookingError.Conflict,
                "The selected room is inactive.");
        }

        var petExists = await dbContext.Pets.AnyAsync(
            pet => pet.Id == request.PetId,
            cancellationToken);

        if (!petExists)
        {
            throw new BookingException(
                BookingError.NotFound,
                "Pet not found.");
        }

        var hasConflict = await dbContext.Stays.AnyAsync(
            stay =>
                stay.RoomId == request.RoomId &&
                (stay.Status == StayStatus.Booked ||
                 stay.Status == StayStatus.CheckedIn) &&
                stay.ScheduledCheckIn < checkOut &&
                stay.ScheduledCheckOut > checkIn,
            cancellationToken);

        if (hasConflict)
        {
            throw new BookingException(
                BookingError.Conflict,
                "The selected room is already booked for these dates.");
        }

        var booking = new Stay
        {
            PetId = request.PetId,
            RoomId = request.RoomId,
            ScheduledCheckIn = checkIn,
            ScheduledCheckOut = checkOut,
            Status = StayStatus.Booked
        };

        dbContext.Stays.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Return timestamps with the precision stored by PostgreSQL.
        await dbContext.Entry(booking).ReloadAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ToResponse(booking);
    }

    private static BookingResponse ToResponse(Stay stay)
    {
        return new BookingResponse
        {
            Id = stay.Id,
            PetId = stay.PetId,
            RoomId = stay.RoomId,
            ScheduledCheckIn = stay.ScheduledCheckIn,
            ScheduledCheckOut = stay.ScheduledCheckOut,
            Status = stay.Status.ToString()
        };
    }
}