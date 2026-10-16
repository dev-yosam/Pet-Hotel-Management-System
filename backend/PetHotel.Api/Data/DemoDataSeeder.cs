using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Models;

namespace PetHotel.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        PetHotelDbContext db,
        CancellationToken cancellationToken = default)
    {
        var demoRooms = new[]
        {
            new Room
            {
                RoomNumber = "DEMO-101",
                RoomType = "Standard",
                IsActive = true
            },
            new Room
            {
                RoomNumber = "DEMO-201",
                RoomType = "Deluxe",
                IsActive = true
            }
        };

        var roomNumbers = demoRooms
            .Select(room => room.RoomNumber)
            .ToArray();

        var existingNumbers = await db.Rooms
            .AsNoTracking()
            .Where(room => roomNumbers.Contains(room.RoomNumber))
            .Select(room => room.RoomNumber)
            .ToListAsync(cancellationToken);

        var missingRooms = demoRooms
            .Where(room => !existingNumbers.Contains(room.RoomNumber))
            .ToList();

        if (missingRooms.Count == 0)
        {
            return;
        }

        db.Rooms.AddRange(missingRooms);
        await db.SaveChangesAsync(cancellationToken);
    }
}