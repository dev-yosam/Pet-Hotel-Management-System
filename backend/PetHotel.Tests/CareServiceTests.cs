using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

public class CareServiceTests
{
    private PetHotelDbContext db = null!;
    private CareService service = null!;
    private readonly DateTimeOffset checkIn = new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<PetHotelDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        db = new PetHotelDbContext(options);
        service = new CareService(db);
        db.Stays.Add(new Stay
        {
            Id = 1, Status = StayStatus.CheckedIn,
            ScheduledCheckIn = checkIn, ActualCheckIn = checkIn, ScheduledCheckOut = checkIn.AddDays(1),
            Pet = new Pet { Name = "Milo", Species = "Dog", Owner = new Owner { FullName = "Sam", Phone = "0400000000" } },
            Room = new Room { RoomNumber = "101", RoomType = "Standard" }
        });
        await db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown() => await db.DisposeAsync();

    [Test]
    public async Task Create_TrimsDescriptionAndStoresUtc()
    {
        var request = new CreateCareTaskRequest
        {
            Description = "  Feed Milo  ", ScheduledAt = checkIn.ToOffset(TimeSpan.FromHours(11))
        };
        var task = await service.CreateTaskAsync(1, request);
        db.ChangeTracker.Clear();
        var stored = await db.CareRecords.SingleAsync();
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
        AssertCareError(() => service.CreateTaskAsync(1, request), CareError.InvalidInput);
    }

    [Test]
    public void Create_RejectsLongDescription()
    {
        var request = Request();
        request.Description = new string('x', 501);
        AssertCareError(() => service.CreateTaskAsync(1, request), CareError.InvalidInput);
    }

    [TestCase(-1)]
    [TestCase(1440)]
    [TestCase(1441)]
    public void Create_RejectsTimeOutsideStay(int minutes)
    {
        var request = Request();
        request.ScheduledAt = checkIn.AddMinutes(minutes);
        AssertCareError(() => service.CreateTaskAsync(1, request), CareError.InvalidInput);
    }

    [Test]
    public void Create_RequiresScheduledTime()
    {
        var request = Request();
        request.ScheduledAt = null;
        AssertCareError(() => service.CreateTaskAsync(1, request), CareError.InvalidInput);
    }

    [TestCase(StayStatus.Booked)]
    [TestCase(StayStatus.CheckedOut)]
    [TestCase(StayStatus.Cancelled)]
    public async Task ClosedOrUnstartedStay_RejectsChangesButAllowsReading(StayStatus status)
    {
        var task = await service.CreateTaskAsync(1, Request());
        var stay = await db.Stays.SingleAsync();
        stay.Status = status;
        await db.SaveChangesAsync();

        AssertCareError(() => service.CreateTaskAsync(1, Request()), CareError.Conflict);
        AssertCareError(() => service.UpdateCompletionAsync(task.Id, true), CareError.Conflict);
        AssertCareError(() => service.DeleteTaskAsync(task.Id), CareError.Conflict);
        Assert.That(await service.GetTasksAsync(1), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Create_RejectsMissingActualCheckIn()
    {
        (await db.Stays.SingleAsync()).ActualCheckIn = null;
        await db.SaveChangesAsync();
        AssertCareError(() => service.CreateTaskAsync(1, Request()), CareError.Conflict);
    }

    [Test]
    public async Task Create_RejectsAlreadyRecordedCheckOut()
    {
        (await db.Stays.SingleAsync()).ActualCheckOut = checkIn.AddHours(2);
        await db.SaveChangesAsync();
        AssertCareError(() => service.CreateTaskAsync(1, Request()), CareError.Conflict);
    }

    [Test]
    public async Task Completion_PreservesFirstTimestampAndCanBeUndone()
    {
        var task = await service.CreateTaskAsync(1, Request());
        var completed = await service.UpdateCompletionAsync(task.Id, true);
        var repeated = await service.UpdateCompletionAsync(task.Id, true);
        Assert.That(completed.CompletedAt, Is.Not.Null);
        Assert.That(repeated.CompletedAt, Is.EqualTo(completed.CompletedAt));

        var undone = await service.UpdateCompletionAsync(task.Id, false);
        Assert.That(undone.IsCompleted, Is.False);
        Assert.That(undone.CompletedAt, Is.Null);
    }

    [Test]
    public async Task Delete_OnlyRemovesUnfinishedTasks()
    {
        var unfinished = await service.CreateTaskAsync(1, Request());
        await service.DeleteTaskAsync(unfinished.Id);
        Assert.That(await service.GetTasksAsync(1), Is.Empty);

        var completed = await service.CreateTaskAsync(1, Request());
        await service.UpdateCompletionAsync(completed.Id, true);
        AssertCareError(() => service.DeleteTaskAsync(completed.Id), CareError.Conflict);
        Assert.That(await service.GetTasksAsync(1), Has.Count.EqualTo(1));
    }

    [Test]
    public void MissingResources_ReturnNotFound()
    {
        AssertCareError(() => service.GetTasksAsync(999), CareError.NotFound);
        AssertCareError(() => service.GetTaskAsync(999), CareError.NotFound);
        AssertCareError(() => service.CreateTaskAsync(999, Request()), CareError.NotFound);
        AssertCareError(() => service.UpdateCompletionAsync(999, true), CareError.NotFound);
        AssertCareError(() => service.DeleteTaskAsync(999), CareError.NotFound);
    }

    [Test]
    public async Task GetTasks_FiltersByStayAndSortsByTime()
    {
        var late = Request();
        late.ScheduledAt = checkIn.AddHours(3);
        await service.CreateTaskAsync(1, late);
        var early = await service.CreateTaskAsync(1, Request());
        db.CareRecords.Add(new CareRecord { StayId = 999, Description = "Other stay", ScheduledAt = checkIn });
        await db.SaveChangesAsync();
        var tasks = await service.GetTasksAsync(1);
        Assert.That(tasks, Has.Count.EqualTo(2));
        Assert.That(tasks[0].Id, Is.EqualTo(early.Id));
    }

    [Test]
    public async Task GetStays_IncludesCheckedInAndHistoricalCareOnly()
    {
        Assert.That(await service.GetStaysAsync(), Has.Count.EqualTo(1));
        var stay = await db.Stays.SingleAsync();
        stay.Status = StayStatus.CheckedOut;
        await db.SaveChangesAsync();
        Assert.That(await service.GetStaysAsync(), Is.Empty);
        db.CareRecords.Add(new CareRecord { StayId = 1, Description = "Past care", ScheduledAt = checkIn });
        await db.SaveChangesAsync();
        var stays = await service.GetStaysAsync();
        Assert.That(stays, Has.Count.EqualTo(1));
        Assert.That(stays[0].Status, Is.EqualTo("CheckedOut"));
        Assert.That(stays[0].PetName, Is.EqualTo("Milo"));
    }

    private CreateCareTaskRequest Request() => new() { Description = "Feed Milo", ScheduledAt = checkIn.AddHours(1) };

    private static void AssertCareError(AsyncTestDelegate action, CareError expected)
    {
        var exception = Assert.ThrowsAsync<CareException>(action);
        Assert.That(exception!.Error, Is.EqualTo(expected));
    }
}
