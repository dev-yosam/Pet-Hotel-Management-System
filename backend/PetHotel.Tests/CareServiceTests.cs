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
    public async Task Create_TrimsDescriptionAndStoresUtc()
    {
        var request = new CreateCareTaskRequest
        {
            Description = "  Feed Milo  ",
            ScheduledAt = checkIn.ToOffset(TimeSpan.FromHours(11))
        };

        var task = await service.CreateTaskAsync(stayId, request);

        db.ChangeTracker.Clear();

        var stored = await db.CareRecords
            .SingleAsync(record => record.StayId == stayId);

        Assert.Multiple(() =>
        {
            Assert.That(stored.Description, Is.EqualTo("Feed Milo"));
            Assert.That(stored.ScheduledAt, Is.EqualTo(checkIn));
            Assert.That(stored.ScheduledAt.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(task.IsCompleted, Is.False);
            Assert.That(task.CompletedAt, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_RejectsEmptyDescription(string? description)
    {
        var request = Request();
        request.Description = description!;

        AssertCareError(
            () => service.CreateTaskAsync(stayId, request),
            CareError.InvalidInput);
    }

    [Test]
    public void Create_RejectsLongDescription()
    {
        var request = Request();
        request.Description = new string('x', 501);

        AssertCareError(
            () => service.CreateTaskAsync(stayId, request),
            CareError.InvalidInput);
    }

    [TestCase(-1)]
    [TestCase(1440)]
    [TestCase(1441)]
    public void Create_RejectsTimeOutsideStay(int minutes)
    {
        var request = Request();
        request.ScheduledAt = checkIn.AddMinutes(minutes);

        AssertCareError(
            () => service.CreateTaskAsync(stayId, request),
            CareError.InvalidInput);
    }

    [Test]
    public void Create_RequiresScheduledTime()
    {
        var request = Request();
        request.ScheduledAt = null;

        AssertCareError(
            () => service.CreateTaskAsync(stayId, request),
            CareError.InvalidInput);
    }

    [TestCase(StayStatus.Booked)]
    [TestCase(StayStatus.CheckedOut)]
    [TestCase(StayStatus.Cancelled)]
    public async Task ClosedOrUnstartedStay_RejectsChangesButAllowsReading(
        StayStatus status)
    {
        var task = await service.CreateTaskAsync(stayId, Request());

        var stay = await db.Stays
            .SingleAsync(stay => stay.Id == stayId);

        stay.Status = status;
        await db.SaveChangesAsync();

        AssertCareError(
            () => service.CreateTaskAsync(stayId, Request()),
            CareError.Conflict);

        AssertCareError(
            () => service.UpdateCompletionAsync(task.Id, true),
            CareError.Conflict);

        AssertCareError(
            () => service.DeleteTaskAsync(task.Id),
            CareError.Conflict);

        Assert.That(
            await service.GetTasksAsync(stayId),
            Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Create_RejectsMissingActualCheckIn()
    {
        var stay = await db.Stays
            .SingleAsync(stay => stay.Id == stayId);

        stay.ActualCheckIn = null;
        await db.SaveChangesAsync();

        AssertCareError(
            () => service.CreateTaskAsync(stayId, Request()),
            CareError.Conflict);
    }

    [Test]
    public async Task Create_RejectsAlreadyRecordedCheckOut()
    {
        var stay = await db.Stays
            .SingleAsync(stay => stay.Id == stayId);

        stay.ActualCheckOut = checkIn.AddHours(2);
        await db.SaveChangesAsync();

        AssertCareError(
            () => service.CreateTaskAsync(stayId, Request()),
            CareError.Conflict);
    }

    [Test]
    public async Task Completion_PreservesFirstTimestampAndCanBeUndone()
    {
        var task = await service.CreateTaskAsync(stayId, Request());

        var completed = await service.UpdateCompletionAsync(task.Id, true);
        var repeated = await service.UpdateCompletionAsync(task.Id, true);

        Assert.That(completed.CompletedAt, Is.Not.Null);
        Assert.That(
            repeated.CompletedAt,
            Is.EqualTo(completed.CompletedAt));

        var undone = await service.UpdateCompletionAsync(task.Id, false);

        Assert.That(undone.IsCompleted, Is.False);
        Assert.That(undone.CompletedAt, Is.Null);
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

    [Test]
    public void MissingResources_ReturnNotFound()
    {
        AssertCareError(
            () => service.GetTasksAsync(-1),
            CareError.NotFound);

        AssertCareError(
            () => service.GetTaskAsync(-1),
            CareError.NotFound);

        AssertCareError(
            () => service.CreateTaskAsync(-1, Request()),
            CareError.NotFound);

        AssertCareError(
            () => service.UpdateCompletionAsync(-1, true),
            CareError.NotFound);

        AssertCareError(
            () => service.DeleteTaskAsync(-1),
            CareError.NotFound);
    }

    [Test]
    public async Task GetTasks_FiltersByStayAndSortsByTime()
    {
        var late = Request();
        late.ScheduledAt = checkIn.AddHours(3);
        await service.CreateTaskAsync(stayId, late);

        var early = await service.CreateTaskAsync(stayId, Request());

        var original = await db.Stays
            .SingleAsync(stay => stay.Id == stayId);

        var otherStay = new Stay
        {
            PetId = original.PetId,
            RoomId = roomId,
            ScheduledCheckIn = checkIn.AddDays(2),
            ScheduledCheckOut = checkIn.AddDays(3),
            Status = StayStatus.Booked
        };

        db.Stays.Add(otherStay);
        db.CareRecords.Add(new CareRecord
        {
            Stay = otherStay,
            Description = "Other stay",
            ScheduledAt = checkIn.AddDays(2)
        });

        await db.SaveChangesAsync();

        var tasks = await service.GetTasksAsync(stayId);

        Assert.That(tasks, Has.Count.EqualTo(2));
        Assert.That(tasks[0].Id, Is.EqualTo(early.Id));
    }

    [Test]
    public async Task GetStays_IncludesCheckedInAndHistoricalCareOnly()
    {
        var initial = await service.GetStaysAsync();

        Assert.That(
            initial.Any(item => item.RoomNumber == roomNumber),
            Is.True);

        var stay = await db.Stays
            .SingleAsync(stay => stay.Id == stayId);

        stay.Status = StayStatus.CheckedOut;
        await db.SaveChangesAsync();

        var withoutCare = await service.GetStaysAsync();

        Assert.That(
            withoutCare.Any(item => item.RoomNumber == roomNumber),
            Is.False);

        db.CareRecords.Add(new CareRecord
        {
            StayId = stayId,
            Description = "Past care",
            ScheduledAt = checkIn
        });

        await db.SaveChangesAsync();

        var stays = await service.GetStaysAsync();
        var historical = stays.Single(
            item => item.RoomNumber == roomNumber);

        Assert.That(historical.Status, Is.EqualTo("CheckedOut"));
        Assert.That(historical.PetName, Is.EqualTo("Milo"));
    }

    [TestCase("create")]
    [TestCase("complete")]
    [TestCase("delete")]
    public async Task CareChange_WhenCheckoutIsUncommitted_WaitsThenRejects(
    string operation)
    {
        var task = await service.CreateTaskAsync(stayId, Request());

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(15));
        var cancellationToken = timeout.Token;

        await using var workerDb = new PetHotelDbContext(options);
        await workerDb.Database.OpenConnectionAsync(cancellationToken);

        var workerConnection =
            (NpgsqlConnection)workerDb.Database.GetDbConnection();
        var workerPid = workerConnection.ProcessID;

        // Deliberately track old values before checkout changes the stay.
        await workerDb.Stays.SingleAsync(
            stay => stay.Id == stayId,
            cancellationToken);
        await workerDb.CareRecords.SingleAsync(
            record => record.Id == task.Id,
            cancellationToken);

        var workerService = new CareService(workerDb);

        await using var checkoutDb = new PetHotelDbContext(options);
        await using var checkoutTransaction =
            await checkoutDb.Database.BeginTransactionAsync(
                cancellationToken);

        var stay = await checkoutDb.Stays.SingleAsync(
            stay => stay.Id == stayId,
            cancellationToken);

        stay.Status = StayStatus.CheckedOut;
        stay.ActualCheckOut = checkIn.AddHours(2);
        stay.TotalCharge = 40m;

        // Save without committing, so PostgreSQL holds the stay's row lock.
        await checkoutDb.SaveChangesAsync(cancellationToken);

        async Task<CareException?> AttemptChangeAsync()
        {
            try
            {
                switch (operation)
                {
                    case "create":
                        await workerService.CreateTaskAsync(
                            stayId, Request(), cancellationToken);
                        break;

                    case "complete":
                        await workerService.UpdateCompletionAsync(
                            task.Id, true, cancellationToken);
                        break;

                    case "delete":
                        await workerService.DeleteTaskAsync(
                            task.Id, cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException(
                            "Unknown test operation.");
                }

                return null;
            }
            catch (CareException exception)
            {
                return exception;
            }
        }

        var pendingChange = AttemptChangeAsync();

        try
        {
            await using var observer = new NpgsqlConnection(
                db.Database.GetConnectionString());
            await observer.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                "SELECT cardinality(pg_blocking_pids(@pid)) > 0",
                observer);
            command.Parameters.AddWithValue("pid", workerPid);

            // Confirm an actual database wait instead of relying on a fixed delay.
            var blocked = false;

            while (!pendingChange.IsCompleted)
            {
                blocked = (bool)(await command.ExecuteScalarAsync(
                    cancellationToken))!;

                if (blocked)
                    break;

                await Task.Delay(20, cancellationToken);
            }

            Assert.That(
                blocked,
                Is.True,
                "The care operation must wait for the checkout transaction.");

            await checkoutTransaction.CommitAsync(cancellationToken);

            var exception = await pendingChange;

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Error, Is.EqualTo(CareError.Conflict));

            await using var verification = new PetHotelDbContext(options);

            var storedStay = await verification.Stays
                .AsNoTracking()
                .SingleAsync(
                    item => item.Id == stayId,
                    cancellationToken);

            var records = await verification.CareRecords
                .AsNoTracking()
                .Where(record => record.StayId == stayId)
                .ToListAsync(cancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(
                    storedStay.Status,
                    Is.EqualTo(StayStatus.CheckedOut));
                Assert.That(
                    storedStay.ActualCheckOut,
                    Is.EqualTo(checkIn.AddHours(2)));
                Assert.That(storedStay.TotalCharge, Is.EqualTo(40m));

                Assert.That(records, Has.Count.EqualTo(1));
                Assert.That(records[0].Id, Is.EqualTo(task.Id));
                Assert.That(records[0].IsCompleted, Is.False);
                Assert.That(records[0].CompletedAt, Is.Null);
            });
        }
        finally
        {
            // Release the lock and finish the worker even if an assertion fails.
            await checkoutTransaction.DisposeAsync();

            try
            {
                await pendingChange;
            }
            catch (OperationCanceledException)
            {
                // The test timeout already reports the failed operation.
            }
        }
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