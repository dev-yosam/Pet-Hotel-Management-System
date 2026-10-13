using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

[NonParallelizable]
public class OwnerServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext dbContext = null!;
    private OwnerService service = null!;

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

        service = new OwnerService(dbContext);
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
    public async Task Create_WithValidRequest_PersistsTrimmedOwner()
    {
        var request = CreateValidRequest();
        request.FullName = "  Test Owner  ";
        request.Phone = "  0400000000  ";

        var response = await service.CreateAsync(request);

        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Owners
            .AsNoTracking()
            .SingleAsync(owner => owner.Id == response.Id);

        Assert.That(response.Id, Is.GreaterThan(0));
        Assert.That(saved.FullName, Is.EqualTo("Test Owner"));
        Assert.That(saved.Phone, Is.EqualTo("0400000000"));
        Assert.That(saved.Email, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task GetById_WhenOwnerExists_ReturnsSavedOwner()
    {
        var created = await service.CreateAsync(CreateValidRequest());
        dbContext.ChangeTracker.Clear();

        var response = await service.GetByIdAsync(created.Id);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Id, Is.EqualTo(created.Id));
        Assert.That(response.FullName, Is.EqualTo("Test Owner"));
        Assert.That(response.Phone, Is.EqualTo("0400000000"));
    }

    [Test]
    public async Task Update_WhenOwnerExists_PersistsChanges()
    {
        var created = await service.CreateAsync(CreateValidRequest());
        dbContext.ChangeTracker.Clear();

        var request = new OwnerRequest
        {
            FullName = "Updated Owner",
            Phone = "0400000001",
            Email = null
        };

        var response = await service.UpdateAsync(created.Id, request);
        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Owners
            .AsNoTracking()
            .SingleAsync(owner => owner.Id == created.Id);

        Assert.That(response, Is.Not.Null);
        Assert.That(saved.FullName, Is.EqualTo("Updated Owner"));
        Assert.That(saved.Phone, Is.EqualTo("0400000001"));
        Assert.That(saved.Email, Is.Null);
    }

    [Test]
    public async Task Update_WhenOwnerDoesNotExist_ReturnsNull()
    {
        var response = await service.UpdateAsync(
            -1,
            CreateValidRequest());

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task Create_WithInvalidName_DoesNotSaveOwner()
    {
        var countBefore = await dbContext.Owners.CountAsync();
        var request = CreateValidRequest();
        request.FullName = "   ";

        Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await service.CreateAsync(request);
        });

        dbContext.ChangeTracker.Clear();
        var countAfter = await dbContext.Owners.CountAsync();

        Assert.That(countAfter, Is.EqualTo(countBefore));
    }

    private static OwnerRequest CreateValidRequest()
    {
        return new OwnerRequest
        {
            FullName = "Test Owner",
            Phone = "0400000000",
            Email = "test@example.com"
        };
    }
}