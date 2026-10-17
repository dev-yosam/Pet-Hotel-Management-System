using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Models;

namespace PetHotel.Api.Data;

public static class RoomSeeder
{
    public static async Task SeedAsync(PetHotelDbContext db,
        CancellationToken cancellationToken = default)
    {
        var existingNumbers = await db.Rooms.Select(room => room.RoomNumber)
            .ToListAsync(cancellationToken);

        // Keep existing rooms and their stay records when the app restarts.
        for (var number = 1; number <= 6; number++)
        {
            AddMissingRoom((100 + number).ToString(), "Standard");
            AddMissingRoom((200 + number).ToString(), "Deluxe");
        }

        await db.SaveChangesAsync(cancellationToken);

        void AddMissingRoom(string roomNumber, string roomType)
        {
            if (!existingNumbers.Contains(roomNumber))
            {
                db.Rooms.Add(new Room { RoomNumber = roomNumber, RoomType = roomType });
            }
        }
    }
}
