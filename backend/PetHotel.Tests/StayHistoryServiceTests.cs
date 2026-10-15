using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

public class StayHistoryServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext db = null!;
    private StayHistoryService service = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<PetHotelDbContext>().Build();
        var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("PetHotel")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:PetHotel in API user secrets."))
        {
            Database = $"pet_hotel_history_tests_{Guid.NewGuid():N}"
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
        service = new StayHistoryService(db);
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
    public async Task EmptyDatabase_ReturnsEmptyList()
    {
        var result = await service.SearchAsync(new());
        Assert.That(result.TotalCount, Is.Zero);
        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public async Task Search_CombinesNamesAndStatusAndReturnsDetails()
    {
        var expected = AddStay("Milo", "Sam Lee", StayStatus.CheckedOut);
        expected.ActualCheckIn = expected.ScheduledCheckIn;
        expected.ActualCheckOut = expected.ScheduledCheckOut;
        expected.TotalCharge = 60m;
        AddStay("Milo", "Sam Lee", StayStatus.Booked);
        AddStay("Milo", "Alex", StayStatus.CheckedOut);
        AddStay("Luna", "Sam Lee", StayStatus.CheckedOut);
        await db.SaveChangesAsync();
        var result = await service.SearchAsync(new() { Pet = " MI ", Owner = " lee ", Status = "CheckedOut" });
        Assert.That(result.TotalCount, Is.EqualTo(1));
        var item = result.Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(item.Id, Is.EqualTo(expected.Id));
            Assert.That(item.PetName, Is.EqualTo("Milo"));
            Assert.That(item.Species, Is.EqualTo("Dog"));
            Assert.That(item.OwnerPhone, Is.EqualTo("0400000000"));
            Assert.That(item.RoomNumber, Is.EqualTo(expected.Room.RoomNumber));
            Assert.That(item.Status, Is.EqualTo("CheckedOut"));
            Assert.That(item.ActualCheckIn, Is.EqualTo(expected.ActualCheckIn));
            Assert.That(item.ActualCheckOut, Is.EqualTo(expected.ActualCheckOut));
            Assert.That(item.TotalCharge, Is.EqualTo(60m));
        });
    }

    [TestCase("2026-10-04", "2026-10-03T14:00:00Z", "2026-10-04T13:00:00Z")]
    [TestCase("2026-04-05", "2026-04-04T13:00:00Z", "2026-04-05T14:00:00Z")]
    public async Task Search_IncludesSydneyDateBoundaries(string date, string startText, string endText)
    {
        var start = DateTimeOffset.Parse(startText);
        var end = DateTimeOffset.Parse(endText);
        foreach (var time in new[] { start.AddSeconds(-1), start, end.AddSeconds(-1), end })
        {
            var stay = AddStay("Milo", "Sam", StayStatus.Booked);
            stay.ScheduledCheckIn = time;
            stay.ScheduledCheckOut = time.AddDays(1);
        }
        await db.SaveChangesAsync();
        var result = await service.SearchAsync(new() { From = DateOnly.Parse(date), To = DateOnly.Parse(date), Sort = "oldest" });
        Assert.That(result.Items.Select(item => item.ScheduledCheckIn), Is.EqualTo(new[] { start, end.AddSeconds(-1) }));
    }

    [TestCase("newest", "Zoe")]
    [TestCase("oldest", "Amy")]
    [TestCase("pet", "Amy")]
    [TestCase("owner", "Zoe")]
    public async Task Search_SortsBeforePaging(string sort, string expectedName)
    {
        var first = AddStay("Amy", "Zoe", StayStatus.Booked);
        var second = AddStay("Zoe", "Amy", StayStatus.Cancelled);
        second.ScheduledCheckIn = first.ScheduledCheckIn.AddHours(1);
        await db.SaveChangesAsync();
        var pageOne = await service.SearchAsync(new() { Sort = sort, PageSize = 1 });
        var pageTwo = await service.SearchAsync(new() { Sort = sort, PageSize = 1, Page = 2 });
        Assert.Multiple(() =>
        {
            Assert.That(pageOne.TotalCount, Is.EqualTo(2));
            Assert.That(pageOne.Items.Single().PetName, Is.EqualTo(expectedName));
            Assert.That(pageTwo.Items.Single().Id, Is.Not.EqualTo(pageOne.Items.Single().Id));
        });
    }

    [Test]
    public async Task Search_EscapesLiteralSearchAndHandlesNoMatch()
    {
        AddStay("Milo", "Sam", StayStatus.Booked);
        await db.SaveChangesAsync();
        var result = await service.SearchAsync(new() { Pet = "%" });
        Assert.That(result.TotalCount, Is.Zero);
    }

    [TestCase("status")]
    [TestCase("sort")]
    [TestCase("page")]
    [TestCase("size")]
    [TestCase("dates")]
    public void Search_RejectsInvalidQuery(string field)
    {
        var query = new StayHistoryQuery();
        switch (field)
        {
            case "status": query.Status = "Unknown"; break;
            case "sort": query.Sort = "Unknown"; break;
            case "page": query.Page = 0; break;
            case "size": query.PageSize = 101; break;
            case "dates":
                query.From = new DateOnly(2026, 10, 16);
                query.To = new DateOnly(2026, 10, 15);
                break;
        }
        var error = Assert.ThrowsAsync<StayException>(() => service.SearchAsync(query));
        Assert.That(error!.Error, Is.EqualTo(StayError.InvalidInput));
    }

    private Stay AddStay(string pet, string owner, StayStatus status)
    {
        var start = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        var stay = new Stay
        {
            Pet = new Pet { Name = pet, Species = "Dog", Owner = new Owner { FullName = owner, Phone = "0400000000" } },
            Room = new Room { RoomNumber = Guid.NewGuid().ToString("N")[..12], RoomType = "Standard" },
            Status = status, ScheduledCheckIn = start, ScheduledCheckOut = start.AddDays(1)
        };
        db.Stays.Add(stay);
        return stay;
    }
}
