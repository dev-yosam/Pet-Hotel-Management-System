using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

[NonParallelizable]
public class StayServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext dbContext = null!;
    private StayService service = null!;
    private int ownerId;
    private int petId;
    private int roomId;
    private int stayId;

    private static readonly DateTimeOffset Now =
        new(2026, 11, 10, 2, 0, 0, TimeSpan.Zero);

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
        petId = 0;
        roomId = 0;
        stayId = 0;

        dbContext = new PetHotelDbContext(options);
        service = new StayService(
            dbContext,
            new FixedTimeProvider(Now));

        var owner = new Owner
        {
            FullName = "Stay Test Owner",
            Phone = "0400000000"
        };

        var pet = new Pet
        {
            Owner = owner,
            Name = "Stay Test Pet",
            Species = "Dog"
        };

        var room = new Room
        {
            RoomNumber = $"S-{Guid.NewGuid().ToString("N")[..12]}",
            RoomType = "Standard",
            IsActive = true
        };

        var stay = new Stay
        {
            Pet = pet,
            Room = room,
            ScheduledCheckIn = Now.AddHours(-1),
            ScheduledCheckOut = Now.AddDays(2),
            Status = StayStatus.Booked
        };

        dbContext.Stays.Add(stay);
        await dbContext.SaveChangesAsync();

        ownerId = owner.Id;
        petId = pet.Id;
        roomId = room.Id;
        stayId = stay.Id;

        dbContext.ChangeTracker.Clear();
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
            await using var cleanup = new PetHotelDbContext(options);

            await cleanup.Stays
                .Where(stay => stay.PetId == petId || stay.RoomId == roomId)
                .ExecuteDeleteAsync();

            await cleanup.Pets
                .Where(pet => pet.Id == petId)
                .ExecuteDeleteAsync();

            await cleanup.Rooms
                .Where(room => room.Id == roomId)
                .ExecuteDeleteAsync();

            await cleanup.Owners
                .Where(owner => owner.Id == ownerId)
                .ExecuteDeleteAsync();
        }
        finally
        {
            await dbContext.DisposeAsync();
        }
    }

    [Test]
    public async Task GetById_WhenStayExists_ReturnsStay()
    {
        var response = await service.GetByIdAsync(stayId);

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.Id, Is.EqualTo(stayId));
            Assert.That(response.PetId, Is.EqualTo(petId));
            Assert.That(response.RoomId, Is.EqualTo(roomId));
            Assert.That(response.Status, Is.EqualTo("Booked"));
            Assert.That(response.ActualCheckIn, Is.Null);
            Assert.That(response.ActualCheckOut, Is.Null);
        });
    }

    [Test]
    public async Task GetById_WhenStayDoesNotExist_ReturnsNull()
    {
        var response = await service.GetByIdAsync(-1);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task CheckIn_WithValidStay_PersistsActualCheckIn()
    {
        var response = await service.CheckInAsync(stayId);

        await using var verification = new PetHotelDbContext(options);
        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("CheckedIn"));
            Assert.That(response.ActualCheckIn, Is.EqualTo(Now));
            Assert.That(saved.Status, Is.EqualTo(StayStatus.CheckedIn));
            Assert.That(saved.ActualCheckIn, Is.EqualTo(Now));
            Assert.That(saved.ActualCheckIn!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(saved.ActualCheckOut, Is.Null);
            Assert.That(
                saved.ScheduledCheckIn,
                Is.EqualTo(Now.AddHours(-1)));
            Assert.That(
                saved.ScheduledCheckOut,
                Is.EqualTo(Now.AddDays(2)));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CheckIn_WithInvalidId_ThrowsInvalidInput(int id)
    {
        var exception = Assert.ThrowsAsync<StayException>(async () =>
        {
            await service.CheckInAsync(id);
        });

        Assert.That(exception!.Error, Is.EqualTo(StayError.InvalidInput));
    }

    [Test]
    public async Task CheckIn_WhenStayDoesNotExist_ThrowsNotFound()
    {
        await dbContext.Stays
            .Where(stay => stay.Id == stayId)
            .ExecuteDeleteAsync();

        var exception = Assert.ThrowsAsync<StayException>(async () =>
        {
            await service.CheckInAsync(stayId);
        });

        Assert.That(exception!.Error, Is.EqualTo(StayError.NotFound));
    }

    [Test]
    public async Task CheckIn_WhenAlreadyCheckedIn_PreservesFirstCheckIn()
    {
        await service.CheckInAsync(stayId);

        var laterService = new StayService(
            dbContext,
            new FixedTimeProvider(Now.AddMinutes(10)));

        var exception = Assert.ThrowsAsync<StayException>(async () =>
        {
            await laterService.CheckInAsync(stayId);
        });

        await using var verification = new PetHotelDbContext(options);
        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error, Is.EqualTo(StayError.Conflict));
            Assert.That(saved.Status, Is.EqualTo(StayStatus.CheckedIn));
            Assert.That(saved.ActualCheckIn, Is.EqualTo(Now));
            Assert.That(saved.ActualCheckOut, Is.Null);
        });
    }

    [TestCase(-1)]
    [TestCase(0)]
    public async Task CheckIn_AtOrAfterScheduledCheckIn_AllowsCheckIn(
    int secondsAfterStart)
    {
        var start = Now.AddSeconds(secondsAfterStart);

        await dbContext.Stays
            .Where(stay => stay.Id == stayId)
            .ExecuteUpdateAsync(update =>
                update.SetProperty(stay => stay.ScheduledCheckIn, start));

        var response = await service.CheckInAsync(stayId);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("CheckedIn"));
            Assert.That(response.ActualCheckIn, Is.EqualTo(Now));
        });
    }

    [Test]
    public async Task CheckIn_BeforeScheduledCheckIn_RejectsCheckIn()
    {
        await dbContext.Stays
            .Where(stay => stay.Id == stayId)
            .ExecuteUpdateAsync(update =>
                update.SetProperty(
                    stay => stay.ScheduledCheckIn,
                    Now.AddSeconds(1)));

        await AssertCheckInRejectedAsync();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task CheckIn_AtOrAfterScheduledCheckOut_RejectsCheckIn(
        int endOffsetSeconds)
    {
        await dbContext.Stays
            .Where(stay => stay.Id == stayId)
            .ExecuteUpdateAsync(update =>
                update.SetProperty(
                    stay => stay.ScheduledCheckOut,
                    Now.AddSeconds(endOffsetSeconds)));

        await AssertCheckInRejectedAsync();
    }

    [Test]
    public async Task CheckIn_WithInactiveRoom_RejectsCheckIn()
    {
        await dbContext.Rooms
            .Where(room => room.Id == roomId)
            .ExecuteUpdateAsync(update =>
                update.SetProperty(room => room.IsActive, false));

        await AssertCheckInRejectedAsync();
    }

    [Test]
    public async Task CheckIn_WhenPreviousStayHasNotCheckedOut_RejectsCheckIn()
    {
        var previousStay = new Stay
        {
            PetId = petId,
            RoomId = roomId,
            ScheduledCheckIn = Now.AddDays(-2),
            ScheduledCheckOut = Now.AddHours(-1),
            ActualCheckIn = Now.AddDays(-2),
            Status = StayStatus.CheckedIn
        };

        dbContext.Stays.Add(previousStay);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        await AssertCheckInRejectedAsync();
    }

    [Test]
    public async Task CheckIn_WhenPreviousStayHasCheckedOut_AllowsCheckIn()
    {
        var previousStay = new Stay
        {
            PetId = petId,
            RoomId = roomId,
            ScheduledCheckIn = Now.AddDays(-2),
            ScheduledCheckOut = Now.AddHours(-1),
            ActualCheckIn = Now.AddDays(-2),
            ActualCheckOut = Now.AddHours(-1),
            Status = StayStatus.CheckedOut
        };

        dbContext.Stays.Add(previousStay);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var response = await service.CheckInAsync(stayId);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("CheckedIn"));
            Assert.That(response.ActualCheckIn, Is.EqualTo(Now));
        });
    }

    private async Task AssertCheckInRejectedAsync()
    {
        var exception = Assert.ThrowsAsync<StayException>(async () =>
        {
            await service.CheckInAsync(stayId);
        });

        await using var verification = new PetHotelDbContext(options);
        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error, Is.EqualTo(StayError.Conflict));
            Assert.That(saved.Status, Is.EqualTo(StayStatus.Booked));
            Assert.That(saved.ActualCheckIn, Is.Null);
            Assert.That(saved.ActualCheckOut, Is.Null);
        });
    }

    [TestCase(StayStatus.Cancelled)]
    [TestCase(StayStatus.CheckedOut)]
    public async Task CheckIn_WithInvalidStatus_PreservesStay(
    StayStatus status)
    {
        var actualCheckIn = status == StayStatus.CheckedOut
            ? (DateTimeOffset?)Now.AddMinutes(-30)
            : null;

        var actualCheckOut = status == StayStatus.CheckedOut
            ? (DateTimeOffset?)Now.AddMinutes(-10)
            : null;

        await dbContext.Stays
            .Where(stay => stay.Id == stayId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(stay => stay.Status, status)
                .SetProperty(stay => stay.ActualCheckIn, actualCheckIn)
                .SetProperty(stay => stay.ActualCheckOut, actualCheckOut));

        var exception = Assert.ThrowsAsync<StayException>(async () =>
        {
            await service.CheckInAsync(stayId);
        });

        await using var verification = new PetHotelDbContext(options);
        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error, Is.EqualTo(StayError.Conflict));
            Assert.That(saved.Status, Is.EqualTo(status));
            Assert.That(saved.ActualCheckIn, Is.EqualTo(actualCheckIn));
            Assert.That(saved.ActualCheckOut, Is.EqualTo(actualCheckOut));
        });
    }

    [Test]
    public async Task CheckIn_WithConcurrentRequests_OnlyOneSucceeds()
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var start = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<StayError?> TryCheckInAsync()
        {
            await using var context = new PetHotelDbContext(options);
            var stayService = new StayService(
                context,
                new FixedTimeProvider(Now));

            await start.Task.WaitAsync(timeout.Token);

            try
            {
                await stayService.CheckInAsync(stayId, timeout.Token);
                return null;
            }
            catch (StayException exception)
            {
                return exception.Error;
            }
        }

        var firstRequest = TryCheckInAsync();
        var secondRequest = TryCheckInAsync();

        start.SetResult(true);

        var results = await Task.WhenAll(firstRequest, secondRequest);

        await using var verification = new PetHotelDbContext(options);
        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.Multiple(() =>
        {
            Assert.That(
                results.Count(result => result is null),
                Is.EqualTo(1));

            Assert.That(
                results.Count(result => result == StayError.Conflict),
                Is.EqualTo(1));

            Assert.That(saved.Status, Is.EqualTo(StayStatus.CheckedIn));
            Assert.That(saved.ActualCheckIn, Is.EqualTo(Now));
            Assert.That(saved.ActualCheckOut, Is.Null);
        });
    }

    [Test]
    public async Task TotalCharge_BeforeSettlement_IsNull()
    {
        await using var verification = new PetHotelDbContext(options);

        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.That(saved.TotalCharge, Is.Null);
    }

    [Test]
    public async Task TotalCharge_WithDecimalAmount_PersistsValue()
    {
        var stay = await dbContext.Stays
            .SingleAsync(stay => stay.Id == stayId);

        stay.TotalCharge = 123.45m;
        await dbContext.SaveChangesAsync();

        await using var verification = new PetHotelDbContext(options);

        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == stayId);

        Assert.That(saved.TotalCharge, Is.EqualTo(123.45m));
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}