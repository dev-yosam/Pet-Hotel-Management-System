using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

public class DashboardServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext db = null!;
    private readonly TestClock clock = new();

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<PetHotelDbContext>().Build();
        var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("PetHotel")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:PetHotel in API user secrets."))
        {
            Database = $"pet_hotel_dashboard_tests_{Guid.NewGuid():N}"
        };
        options = new DbContextOptionsBuilder<PetHotelDbContext>().UseNpgsql(connection.ConnectionString).Options;
        await using var context = new PetHotelDbContext(options);
        await context.Database.MigrateAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        db = new PetHotelDbContext(options);
        await db.Database.BeginTransactionAsync();
        clock.Now = DateTimeOffset.Parse("2026-10-15T01:00:00Z");
    }

    [TearDown]
    public async Task TearDown()
    {
        await db.Database.RollbackTransactionAsync();
        await db.DisposeAsync();
    }

    [OneTimeTearDown]
    public async Task DeleteDatabase()
    {
        if (options is null) return;
        await using var context = new PetHotelDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task EmptyDatabase_ReturnsZeroCountsAndPercentages()
    {
        var result = await new DashboardService(db, clock).GetSummaryAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Date, Is.EqualTo(new DateOnly(2026, 10, 15)));
            Assert.That(result.CurrentPets, Is.Zero);
            Assert.That(result.ActiveRooms, Is.Zero);
            Assert.That(result.OccupiedRooms, Is.Zero);
            Assert.That(result.OccupancyPercent, Is.Zero);
            Assert.That(result.TodayCheckIns, Is.Zero);
            Assert.That(result.TodayCheckOuts, Is.Zero);
            Assert.That(result.CareTasks, Is.Zero);
            Assert.That(result.CareCompletionPercent, Is.Zero);
        });
    }

    [Test]
    public async Task Summary_UsesActualStaysActiveRoomsAndScheduledCare()
    {
        var occupied = new Room { RoomNumber = "101", RoomType = "Standard" };
        var inactive = new Room { RoomNumber = "102", RoomType = "Standard", IsActive = false };
        db.Rooms.AddRange(occupied, inactive,
            new Room { RoomNumber = "103", RoomType = "Standard" },
            new Room { RoomNumber = "104", RoomType = "Standard" });
        var first = AddStay(occupied, StayStatus.CheckedIn, clock.Now.AddDays(-1));
        // Duplicate rows for one pet must not inflate the pet or room count.
        var duplicate = AddStay(occupied, StayStatus.CheckedIn, clock.Now);
        duplicate.Pet = first.Pet;
        AddStay(inactive, StayStatus.CheckedIn, clock.Now.AddDays(-1));
        AddStay(occupied, StayStatus.Booked, null);
        AddStay(occupied, StayStatus.Cancelled, null);
        var departed = AddStay(occupied, StayStatus.CheckedOut, clock.Now.AddDays(-1));
        departed.ActualCheckOut = clock.Now;
        AddCare(first, clock.Now, true);
        AddCare(first, clock.Now, false);
        AddCare(first, clock.Now, false);
        AddCare(first, clock.Now.AddDays(-1), true);
        await db.SaveChangesAsync();

        var result = await new DashboardService(db, clock).GetSummaryAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.CurrentPets, Is.EqualTo(2));
            Assert.That(result.ActiveRooms, Is.EqualTo(3));
            Assert.That(result.OccupiedRooms, Is.EqualTo(1));
            Assert.That(result.OccupancyPercent, Is.EqualTo(33.3m));
            Assert.That(result.TodayCheckIns, Is.EqualTo(1));
            Assert.That(result.TodayCheckOuts, Is.EqualTo(1));
            Assert.That(result.CareTasks, Is.EqualTo(3));
            Assert.That(result.CompletedCareTasks, Is.EqualTo(1));
            Assert.That(result.PendingCareTasks, Is.EqualTo(2));
            Assert.That(result.CareCompletionPercent, Is.EqualTo(33.3m));
        });
    }

    [TestCase("2026-10-14T13:00:00Z", "2026-10-15T13:00:00Z", "2026-10-15")]
    [TestCase("2026-10-03T14:00:00Z", "2026-10-04T13:00:00Z", "2026-10-04")]
    [TestCase("2026-04-04T13:00:00Z", "2026-04-05T14:00:00Z", "2026-04-05")]
    public async Task Summary_UsesSydneyMidnightIncludingDaylightSaving(string startText, string endText, string date)
    {
        var start = DateTimeOffset.Parse(startText);
        var end = DateTimeOffset.Parse(endText);
        clock.Now = start.AddMinutes(30);
        var room = new Room { RoomNumber = "101", RoomType = "Standard" };
        foreach (var time in new[] { start.AddSeconds(-1), start, end.AddSeconds(-1), end })
        {
            var stay = AddStay(room, StayStatus.CheckedOut, time);
            stay.ActualCheckOut = time;
            AddCare(stay, time, true);
        }
        await db.SaveChangesAsync();
        var result = await new DashboardService(db, clock).GetSummaryAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Date, Is.EqualTo(DateOnly.Parse(date)));
            Assert.That(result.TodayCheckIns, Is.EqualTo(2));
            Assert.That(result.TodayCheckOuts, Is.EqualTo(2));
            Assert.That(result.CareTasks, Is.EqualTo(2));
            Assert.That(result.CompletedCareTasks, Is.EqualTo(2));
            Assert.That(result.CareCompletionPercent, Is.EqualTo(100));
        });
    }

    [Test]
    public async Task Rooms_IncludeAllTwelveAndOnlyCurrentOccupants()
    {
        await RoomSeeder.SeedAsync(db);
        var rooms = await db.Rooms.OrderBy(room => room.RoomNumber).ToListAsync();
        AddStay(rooms[0], StayStatus.CheckedIn, clock.Now);
        AddStay(rooms[1], StayStatus.Booked, null);
        var departed = AddStay(rooms[2], StayStatus.CheckedOut, clock.Now.AddDays(-1));
        departed.ActualCheckOut = clock.Now;
        AddStay(rooms[3], StayStatus.Cancelled, null);
        await db.SaveChangesAsync();

        var service = new DashboardService(db, clock);
        var summary = await service.GetSummaryAsync();
        Assert.Multiple(() =>
        {
            Assert.That(summary.Rooms.Count, Is.EqualTo(12));
            Assert.That(summary.Rooms.Select(room => room.RoomNumber),
                Is.EqualTo(rooms.Select(room => room.RoomNumber)));
            Assert.That(summary.Rooms[0].PetName, Is.EqualTo("Milo"));
            Assert.That(summary.Rooms.Skip(1).All(room => room.PetName is null), Is.True);
        });

        var current = await db.Stays.SingleAsync(stay => stay.Status == StayStatus.CheckedIn);
        current.Status = StayStatus.CheckedOut;
        current.ActualCheckOut = clock.Now;
        await db.SaveChangesAsync();
        var updated = await service.GetSummaryAsync();
        Assert.That(updated.Rooms.All(room => room.PetName is null), Is.True);
    }

    private Stay AddStay(Room room, StayStatus status, DateTimeOffset? checkIn)
    {
        var stay = new Stay
        {
            Room = room, Status = status, ActualCheckIn = checkIn,
            ScheduledCheckIn = clock.Now.AddDays(-2), ScheduledCheckOut = clock.Now.AddDays(2),
            Pet = new Pet
            {
                Name = "Milo", Species = "Dog",
                Owner = new Owner { FullName = "Sam", Phone = "0400000000" }
            }
        };
        db.Stays.Add(stay);
        return stay;
    }

    private void AddCare(Stay stay, DateTimeOffset scheduledAt, bool completed)
    {
        db.CareRecords.Add(new CareRecord
        {
            Stay = stay, Description = "Feed pet", ScheduledAt = scheduledAt,
            IsCompleted = completed, CompletedAt = completed ? clock.Now : null
        });
    }

    private class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
