using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

[NonParallelizable]
public class CareServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext db = null!;
    private CareService service = null!;

    private int stayId;
    private int ownerId;
    private int roomId;
    private string roomNumber = string.Empty;

    private readonly DateTimeOffset checkIn =
        new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);

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
        ownerId = 0;
        roomId = 0;
        stayId = 0;

        db = new PetHotelDbContext(options);
        service = new CareService(db);
        roomNumber = $"CT-{Guid.NewGuid():N}"[..20];

        var stay = new Stay
        {
            Status = StayStatus.CheckedIn,
            ScheduledCheckIn = checkIn,
            ActualCheckIn = checkIn,
            ScheduledCheckOut = checkIn.AddDays(1),
            Pet = new Pet
            {
                Name = "Milo",
                Species = "Dog",
                Owner = new Owner
                {
                    FullName = "Care Test Owner",
                    Phone = "0400000000"
                }
            },
            Room = new Room
            {
                RoomNumber = roomNumber,
                RoomType = "Standard"
            }
        };

        db.Stays.Add(stay);
        await db.SaveChangesAsync();

        stayId = stay.Id;
        ownerId = stay.Pet.OwnerId;
        roomId = stay.RoomId;
    }

    [TearDown]
    public async Task TearDown()
    {
        if (db is null)
        {
            return;
        }

        try
        {
            if (ownerId > 0)
            {
                await using var cleanup = new PetHotelDbContext(options);

                await cleanup.CareRecords
                    .Where(record => record.Stay.Pet.OwnerId == ownerId)
                    .ExecuteDeleteAsync();

                await cleanup.Stays
                    .Where(stay => stay.Pet.OwnerId == ownerId)
                    .ExecuteDeleteAsync();

                await cleanup.Pets
                    .Where(pet => pet.OwnerId == ownerId)
                    .ExecuteDeleteAsync();

                await cleanup.Rooms
                    .Where(room => room.Id == roomId)
                    .ExecuteDeleteAsync();

                await cleanup.Owners
                    .Where(owner => owner.Id == ownerId)
                    .ExecuteDeleteAsync();
            }
        }
        finally
        {
            await db.DisposeAsync();
        }
    }

    [Test]
    public async Task Delete_OnlyRemovesUnfinishedTasks()
    {
        var unfinished = await service.CreateTaskAsync(stayId, Request());
        await service.DeleteTaskAsync(unfinished.Id);

        Assert.That(await service.GetTasksAsync(stayId), Is.Empty);

        var completed = await service.CreateTaskAsync(stayId, Request());
        await service.UpdateCompletionAsync(completed.Id, true);

        AssertCareError(
            () => service.DeleteTaskAsync(completed.Id),
            CareError.Conflict);

        Assert.That(
            await service.GetTasksAsync(stayId),
            Has.Count.EqualTo(1));
    }

    private CreateCareTaskRequest Request() => new()
    {
        Description = "Feed Milo",
        ScheduledAt = checkIn.AddHours(1)
    };

    private static void AssertCareError(
        AsyncTestDelegate action,
        CareError expected)
    {
        var exception = Assert.ThrowsAsync<CareException>(action);

        Assert.That(exception!.Error, Is.EqualTo(expected));
    }
}