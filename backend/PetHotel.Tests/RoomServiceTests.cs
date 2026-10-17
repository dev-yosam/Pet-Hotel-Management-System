using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

[NonParallelizable]
public class RoomServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext dbContext = null!;
    private RoomService service = null!;

    private static readonly DateTimeOffset CheckIn =
        new(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset CheckOut = CheckIn.AddDays(2);

    [OneTimeSetUp]
    public async Task InitializeDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PetHotelDbContext>()
            .Build();

        var connectionString = configuration.GetConnectionString("PetHotel")
            ?? throw new InvalidOperationException(
                "Configure ConnectionStrings:PetHotel in API user secrets.");

        var connectionBuilder = new NpgsqlConnectionStringBuilder(
            connectionString)
        {
            Database = "pet_hotel_tests"
        };

        options = new DbContextOptionsBuilder<PetHotelDbContext>()
            .UseNpgsql(connectionBuilder.ConnectionString)
            .Options;

        await using var context = new PetHotelDbContext(options);
        await context.Database.MigrateAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        dbContext = new PetHotelDbContext(options);
        await dbContext.Database.BeginTransactionAsync();

        service = new RoomService(dbContext);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (dbContext is null)
        {
            return;
        }

        try
        {
            if (dbContext.Database.CurrentTransaction is not null)
            {
                await dbContext.Database.RollbackTransactionAsync();
            }
        }
        finally
        {
            await dbContext.DisposeAsync();
        }
    }

    [Test]
    public async Task GetAll_ReturnsActiveAndInactiveRoomsInOrder()
    {
        var prefix = Guid.NewGuid().ToString("N")[..8];
        var second = await AddRoomAsync($"{prefix}-02", false);
        var first = await AddRoomAsync($"{prefix}-01");

        dbContext.ChangeTracker.Clear();

        var response = await service.GetAllAsync();
        var rooms = response
            .Where(room => room.Id == first.Id || room.Id == second.Id)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(
                rooms.Select(room => room.RoomNumber),
                Is.EqualTo(new[] { first.RoomNumber, second.RoomNumber }));
            Assert.That(rooms[0].RoomType, Is.EqualTo("Standard"));
            Assert.That(rooms[0].IsActive, Is.True);
            Assert.That(rooms[1].IsActive, Is.False);
        });
    }

    [Test]
    public async Task GetAvailable_ExcludesInactiveRooms()
    {
        var active = await AddRoomAsync();
        var inactive = await AddRoomAsync(isActive: false);

        dbContext.ChangeTracker.Clear();

        var response = await service.GetAvailableAsync(CheckIn, CheckOut);
        var ids = response.Select(room => room.Id).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(ids, Does.Contain(active.Id));
            Assert.That(ids, Does.Not.Contain(inactive.Id));
        });
    }

    [TestCase(StayStatus.Booked, false)]
    [TestCase(StayStatus.CheckedIn, false)]
    [TestCase(StayStatus.CheckedOut, true)]
    [TestCase(StayStatus.Cancelled, true)]
    public async Task GetAvailable_RespectsStayStatus(
        StayStatus status,
        bool expectedAvailable)
    {
        var room = await AddRoomAsync();
        await AddStayAsync(room, CheckIn, CheckOut, status);

        dbContext.ChangeTracker.Clear();

        var response = await service.GetAvailableAsync(CheckIn, CheckOut);

        Assert.That(
            response.Any(item => item.Id == room.Id),
            Is.EqualTo(expectedAvailable));
    }

    [TestCase(-1, 1, false)]
    [TestCase(1, 3, false)]
    [TestCase(-1, 3, false)]
    [TestCase(0.5, 1.5, false)]
    [TestCase(-2, 0, true)]
    [TestCase(2, 4, true)]
    public async Task GetAvailable_HandlesOverlapAndAdjacentDates(
        double startDays,
        double endDays,
        bool expectedAvailable)
    {
        var room = await AddRoomAsync();

        await AddStayAsync(
            room,
            CheckIn.AddDays(startDays),
            CheckIn.AddDays(endDays));

        dbContext.ChangeTracker.Clear();

        var response = await service.GetAvailableAsync(CheckIn, CheckOut);

        Assert.That(
            response.Any(item => item.Id == room.Id),
            Is.EqualTo(expectedAvailable));
    }

    [Test]
    public async Task GetAvailable_WithDifferentOffsets_DetectsSamePeriod()
    {
        var room = await AddRoomAsync();
        await AddStayAsync(room, CheckIn, CheckOut);

        dbContext.ChangeTracker.Clear();

        var offset = TimeSpan.FromHours(11);
        var response = await service.GetAvailableAsync(
            CheckIn.ToOffset(offset),
            CheckOut.ToOffset(offset));

        Assert.That(
            response.Any(item => item.Id == room.Id),
            Is.False);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetAvailable_WithInvalidDates_ThrowsValidationException(
        int stayDays)
    {
        Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await service.GetAvailableAsync(
                CheckIn,
                CheckIn.AddDays(stayDays));
        });
    }

    [Test]
    public async Task SeedRooms_CreatesSixOfEachTypeWithoutDuplicates()
    {
        await RoomSeeder.SeedAsync(dbContext);
        var numbers = Enumerable.Range(101, 6).Concat(Enumerable.Range(201, 6))
            .Select(number => number.ToString()).ToArray();
        var firstIds = await dbContext.Rooms.Where(room => numbers.Contains(room.RoomNumber))
            .OrderBy(room => room.RoomNumber).Select(room => room.Id).ToListAsync();
        await RoomSeeder.SeedAsync(dbContext);
        var rooms = await dbContext.Rooms.Where(room => numbers.Contains(room.RoomNumber))
            .OrderBy(room => room.RoomNumber).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(rooms.Count, Is.EqualTo(12));
            Assert.That(rooms.Count(room => room.RoomType == "Standard"), Is.EqualTo(6));
            Assert.That(rooms.Count(room => room.RoomType == "Deluxe"), Is.EqualTo(6));
            Assert.That(rooms.Select(room => room.Id), Is.EqualTo(firstIds));
        });
    }

    private async Task<Room> AddRoomAsync(
        string? roomNumber = null,
        bool isActive = true)
    {
        var room = new Room
        {
            RoomNumber = roomNumber
                ?? $"T-{Guid.NewGuid().ToString("N")[..12]}",
            RoomType = "Standard",
            IsActive = isActive
        };

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        return room;
    }

    private async Task AddStayAsync(
        Room room,
        DateTimeOffset checkIn,
        DateTimeOffset checkOut,
        StayStatus status = StayStatus.Booked)
    {
        var owner = new Owner
        {
            FullName = "Room Test Owner",
            Phone = "0400000000"
        };

        var pet = new Pet
        {
            Owner = owner,
            Name = "Room Test Pet",
            Species = "Dog"
        };

        var stay = new Stay
        {
            Pet = pet,
            Room = room,
            ScheduledCheckIn = checkIn.ToUniversalTime(),
            ScheduledCheckOut = checkOut.ToUniversalTime(),
            Status = status
        };

        if (status is StayStatus.CheckedIn or StayStatus.CheckedOut)
        {
            stay.ActualCheckIn = stay.ScheduledCheckIn;
        }

        if (status == StayStatus.CheckedOut)
        {
            stay.ActualCheckOut = stay.ScheduledCheckOut;
        }

        dbContext.Stays.Add(stay);
        await dbContext.SaveChangesAsync();
    }
}
