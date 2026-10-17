using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class StayHistoryService(PetHotelDbContext db) : IStayHistoryService
{
    private static readonly TimeZoneInfo HotelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    public async Task<StayHistoryResult> SearchAsync(StayHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(query, new ValidationContext(query), errors, true))
            throw new StayException(StayError.InvalidInput, errors[0].ErrorMessage!);

        var stays = db.Stays.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Pet))
        {
            var name = query.Pet.Trim().ToLower();
            stays = stays.Where(stay => stay.Pet.Name.ToLower().Contains(name));
        }
        if (!string.IsNullOrWhiteSpace(query.Owner))
        {
            var name = query.Owner.Trim().ToLower();
            stays = stays.Where(stay => stay.Pet.Owner.FullName.ToLower().Contains(name));
        }
        if (!string.IsNullOrEmpty(query.Status))
        {
            var status = Enum.Parse<StayStatus>(query.Status);
            stays = stays.Where(stay => stay.Status == status);
        }
        // Search planned arrival dates in Sydney, including both selected dates.
        if (query.From is { } from)
        {
            var start = MidnightUtc(from);
            stays = stays.Where(stay => stay.ScheduledCheckIn >= start);
        }
        if (query.To is { } to)
        {
            var end = MidnightUtc(to.AddDays(1));
            stays = stays.Where(stay => stay.ScheduledCheckIn < end);
        }
        var total = await stays.CountAsync(cancellationToken);
        // Use the ID to keep page order stable when names or dates are the same.
        var sorted = query.Sort switch
        {
            "oldest" => stays.OrderBy(stay => stay.ScheduledCheckIn).ThenBy(stay => stay.Id),
            "pet" => stays.OrderBy(stay => stay.Pet.Name).ThenBy(stay => stay.Id),
            "owner" => stays.OrderBy(stay => stay.Pet.Owner.FullName).ThenBy(stay => stay.Id),
            _ => stays.OrderByDescending(stay => stay.ScheduledCheckIn).ThenByDescending(stay => stay.Id)
        };
        var items = await sorted.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(stay => new StayHistoryItem(
                stay.Id, stay.Pet.Name, stay.Pet.Species, stay.Pet.Owner.FullName, stay.Pet.Owner.Phone,
                stay.Room.RoomNumber, stay.Room.RoomType, stay.Status.ToString(),
                stay.ScheduledCheckIn, stay.ScheduledCheckOut, stay.ActualCheckIn, stay.ActualCheckOut,
                stay.TotalCharge))
            .ToListAsync(cancellationToken);
        return new StayHistoryResult(items, total, query.Page, query.PageSize);
    }

    private static DateTimeOffset MidnightUtc(DateOnly date) =>
        new(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), HotelTimeZone));
}
