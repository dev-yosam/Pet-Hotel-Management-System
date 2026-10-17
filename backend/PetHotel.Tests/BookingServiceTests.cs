using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

[NonParallelizable]
public class BookingServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext dbContext = null!;
    private BookingService service = null!;
    private int ownerId;
    private int petId;
    private int roomId;

    private static readonly DateTimeOffset CheckIn =
        new(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

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

        dbContext = new PetHotelDbContext(options);
        service = new BookingService(dbContext);

        var owner = new Owner
        {
            FullName = "Booking Test Owner",
            Phone = "0400000000"
        };

        var pet = new Pet
        {
            Owner = owner,
            Name = "Booking Test Pet",
            Species = "Dog"
        };

        var room = new Room
        {
            RoomNumber = $"B-{Guid.NewGuid().ToString("N")[..12]}",
            RoomType = "Standard",
            IsActive = true
        };

        dbContext.Pets.Add(pet);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        ownerId = owner.Id;
        petId = pet.Id;
        roomId = room.Id;

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
    public async Task Create_WithValidRequest_PersistsBookedStayInUtc()
    {
        var request = CreateValidRequest();
        var offset = TimeSpan.FromHours(11);
        request.ScheduledCheckIn = CheckIn.ToOffset(offset);
        request.ScheduledCheckOut = CheckIn.AddDays(2).ToOffset(offset);

        var response = await service.CreateAsync(request);

        await using var verification = new PetHotelDbContext(options);
        var saved = await verification.Stays
            .AsNoTracking()
            .SingleAsync(stay => stay.Id == response.Id);

        Assert.Multiple(() =>
        {
            Assert.That(response.Id, Is.GreaterThan(0));
            Assert.That(response.Status, Is.EqualTo("Booked"));
            Assert.That(saved.PetId, Is.EqualTo(petId));
            Assert.That(saved.RoomId, Is.EqualTo(roomId));
            Assert.That(saved.Status, Is.EqualTo(StayStatus.Booked));
            Assert.That(saved.ScheduledCheckIn, Is.EqualTo(CheckIn));
            Assert.That(saved.ScheduledCheckOut, Is.EqualTo(CheckIn.AddDays(2)));
            Assert.That(saved.ScheduledCheckIn.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(saved.ScheduledCheckOut.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(saved.ActualCheckIn, Is.Null);
            Assert.That(saved.ActualCheckOut, Is.Null);
        });
    }

    [Test]
    public async Task GetById_WhenBookingExists_ReturnsBooking()
    {
        var created = await service.CreateAsync(CreateValidRequest());
        dbContext.ChangeTracker.Clear();

        var response = await service.GetByIdAsync(created.Id);

        Assert.That(response, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(response!.Id, Is.EqualTo(created.Id));
            Assert.That(response.PetId, Is.EqualTo(petId));
            Assert.That(response.RoomId, Is.EqualTo(roomId));
            Assert.That(response.Status, Is.EqualTo("Booked"));
        });
    }

    [Test]
    public async Task GetById_WhenBookingDoesNotExist_ReturnsNull()
    {
        var response = await service.GetByIdAsync(-1);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task Create_WithInvalidDates_DoesNotSaveBooking()
    {
        var request = CreateValidRequest();
        request.ScheduledCheckOut = request.ScheduledCheckIn;

        var exception = Assert.ThrowsAsync<BookingException>(async () =>
        {
            await service.CreateAsync(request);
        });

        Assert.That(exception!.Error, Is.EqualTo(BookingError.InvalidInput));
        await AssertNoBookingAsync();
    }

    [Test]
    public async Task Create_WithInactiveRoom_DoesNotSaveBooking()
    {
        await dbContext.Rooms
            .Where(room => room.Id == roomId)
            .ExecuteUpdateAsync(update =>
                update.SetProperty(room => room.IsActive, false));

        var exception = Assert.ThrowsAsync<BookingException>(async () =>
        {
            await service.CreateAsync(CreateValidRequest());
        });

        Assert.That(exception!.Error, Is.EqualTo(BookingError.Conflict));
        await AssertNoBookingAsync();
    }

    [Test]
    public async Task Create_WithMissingRoom_DoesNotSaveBooking()
    {
        await dbContext.Rooms
            .Where(room => room.Id == roomId)
            .ExecuteDeleteAsync();

        var exception = Assert.ThrowsAsync<BookingException>(async () =>
        {
            await service.CreateAsync(CreateValidRequest());
        });

        Assert.That(exception!.Error, Is.EqualTo(BookingError.NotFound));
        await AssertNoBookingAsync();
    }

    [Test]
    public async Task Create_WithMissingPet_DoesNotSaveBooking()
    {
        await dbContext.Pets
            .Where(pet => pet.Id == petId)
            .ExecuteDeleteAsync();

        var exception = Assert.ThrowsAsync<BookingException>(async () =>
        {
            await service.CreateAsync(CreateValidRequest());
        });

        Assert.That(exception!.Error, Is.EqualTo(BookingError.NotFound));
        await AssertNoBookingAsync();
    }

    [TestCase(StayStatus.Booked)]
    [TestCase(StayStatus.CheckedIn)]
    public async Task Create_WithOverlappingActiveStay_RejectsBooking(
        StayStatus status)
    {
        await AddExistingStayAsync(CheckIn, CheckIn.AddDays(2), status);

        var exception = Assert.ThrowsAsync<BookingException>(async () =>
        {
            await service.CreateAsync(CreateValidRequest());
        });

        await using var verification = new PetHotelDbContext(options);
        var count = await verification.Stays
            .CountAsync(stay => stay.RoomId == roomId);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error, Is.EqualTo(BookingError.Conflict));
            Assert.That(count, Is.EqualTo(1));
        });
    }

    [TestCase(StayStatus.Cancelled)]
    [TestCase(StayStatus.CheckedOut)]
    public async Task Create_WithOverlappingInactiveStay_AllowsBooking(
        StayStatus status)
    {
        await AddExistingStayAsync(CheckIn, CheckIn.AddDays(2), status);

        var response = await service.CreateAsync(CreateValidRequest());

        await using var verification = new PetHotelDbContext(options);
        var count = await verification.Stays
            .CountAsync(stay => stay.RoomId == roomId);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Booked"));
            Assert.That(count, Is.EqualTo(2));
        });
    }

    [TestCase(-1, 1)]
    [TestCase(1, 3)]
    [TestCase(-1, 3)]
    [TestCase(0.5, 1.5)]
    public async Task Create_WithPartialOrContainingOverlap_RejectsBooking(
        double startDays,
        double endDays)
    {
        await AddExistingStayAsync(
            CheckIn.AddDays(startDays),
            CheckIn.AddDays(endDays));

        var exception = Assert.ThrowsAsync<BookingException>(async () =>
        {
            await service.CreateAsync(CreateValidRequest());
        });

        await using var verification = new PetHotelDbContext(options);
        var count = await verification.Stays
            .CountAsync(stay => stay.RoomId == roomId);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error, Is.EqualTo(BookingError.Conflict));
            Assert.That(count, Is.EqualTo(1));
        });
    }

    [TestCase(-2, 0)]
    [TestCase(2, 4)]
    public async Task Create_WithAdjacentStay_AllowsBooking(
        int startDays,
        int endDays)
    {
        await AddExistingStayAsync(
            CheckIn.AddDays(startDays),
            CheckIn.AddDays(endDays));

        var response = await service.CreateAsync(CreateValidRequest());

        await using var verification = new PetHotelDbContext(options);
        var count = await verification.Stays
            .CountAsync(stay => stay.RoomId == roomId);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo("Booked"));
            Assert.That(count, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Create_WithConcurrentRequests_OnlyOneBookingSucceeds()
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var start = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<BookingError?> TryCreateAsync()
        {
            await using var context = new PetHotelDbContext(options);
            var bookingService = new BookingService(context);

            await start.Task.WaitAsync(timeout.Token);

            try
            {
                await bookingService.CreateAsync(
                    CreateValidRequest(),
                    timeout.Token);

                return null;
            }
            catch (BookingException exception)
            {
                return exception.Error;
            }
        }

        var firstRequest = TryCreateAsync();
        var secondRequest = TryCreateAsync();

        start.SetResult(true);

        var results = await Task.WhenAll(firstRequest, secondRequest);

        await using var verification = new PetHotelDbContext(options);
        var bookings = await verification.Stays
            .AsNoTracking()
            .Where(stay => stay.RoomId == roomId)
            .ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                results.Count(result => result is null),
                Is.EqualTo(1));

            Assert.That(
                results.Count(result => result == BookingError.Conflict),
                Is.EqualTo(1));

            Assert.That(bookings, Has.Count.EqualTo(1));
        });

        Assert.That(bookings[0].Status, Is.EqualTo(StayStatus.Booked));
    }

    [TestCase(StayStatus.Booked)]
    [TestCase(StayStatus.CheckedIn)]
    public async Task SamePet_OverlappingDifferentRoom_IsRejected(StayStatus status)
    {
        var room = new Room { RoomNumber = Guid.NewGuid().ToString("N")[..12], RoomType = "Standard" };
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();
        try
        {
            await AddExistingStayAsync(CheckIn, CheckIn.AddDays(2), status);
            var request = CreateValidRequest();
            request.RoomId = room.Id;
            var error = Assert.ThrowsAsync<BookingException>(() => service.CreateAsync(request));
            Assert.That(error!.Error, Is.EqualTo(BookingError.Conflict));
        }
        finally { await dbContext.Rooms.Where(item => item.Id == room.Id).ExecuteDeleteAsync(); }
    }

    [Test]
    public async Task SamePet_ConcurrentDifferentRooms_OnlyOneSucceeds()
    {
        var room = new Room { RoomNumber = Guid.NewGuid().ToString("N")[..12], RoomType = "Standard" };
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Book(int id)
        {
            await using var context = new PetHotelDbContext(options);
            var request = CreateValidRequest();
            request.RoomId = id;
            await start.Task.WaitAsync(timeout.Token);
            try { await new BookingService(context).CreateAsync(request, timeout.Token); return true; }
            catch (BookingException exception) when (exception.Error == BookingError.Conflict) { return false; }
        }
        try
        {
            var first = Book(roomId);
            var second = Book(room.Id);
            start.SetResult(true);
            var results = await Task.WhenAll(first, second);
            Assert.That(results.Count(success => success), Is.EqualTo(1));
            Assert.That(await dbContext.Stays.CountAsync(stay => stay.PetId == petId), Is.EqualTo(1));
        }
        finally
        {
            await dbContext.Stays.Where(stay => stay.PetId == petId).ExecuteDeleteAsync();
            await dbContext.Rooms.Where(item => item.Id == room.Id).ExecuteDeleteAsync();
        }
    }

    [Test]
    public void PastBooking_IsRejected()
    {
        var request = CreateValidRequest();
        request.ScheduledCheckIn = DateTimeOffset.UtcNow.AddDays(-2);
        request.ScheduledCheckOut = DateTimeOffset.UtcNow.AddDays(-1);
        var error = Assert.ThrowsAsync<BookingException>(() => service.CreateAsync(request));
        Assert.That(error!.Error, Is.EqualTo(BookingError.InvalidInput));
    }

    private async Task AddExistingStayAsync(
        DateTimeOffset checkIn,
        DateTimeOffset checkOut,
        StayStatus status = StayStatus.Booked)
    {
        var stay = new Stay
        {
            PetId = petId,
            RoomId = roomId,
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
        dbContext.ChangeTracker.Clear();
    }

    private BookingRequest CreateValidRequest()
    {
        return new BookingRequest
        {
            PetId = petId,
            RoomId = roomId,
            ScheduledCheckIn = CheckIn,
            ScheduledCheckOut = CheckIn.AddDays(2)
        };
    }

    private async Task AssertNoBookingAsync()
    {
        await using var verification = new PetHotelDbContext(options);

        var exists = await verification.Stays
            .AnyAsync(stay => stay.PetId == petId);

        Assert.That(exists, Is.False);
    }
}
