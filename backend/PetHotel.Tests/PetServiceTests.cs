using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Models;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

[NonParallelizable]
public class PetServiceTests
{
    private DbContextOptions<PetHotelDbContext> options = null!;
    private PetHotelDbContext dbContext = null!;
    private PetService service = null!;
    private int ownerId;

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

        var owner = new Owner
        {
            FullName = "Pet Test Owner",
            Phone = "0400000000"
        };

        dbContext.Owners.Add(owner);
        await dbContext.SaveChangesAsync();

        ownerId = owner.Id;
        service = new PetService(dbContext);
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
    public async Task Create_WithValidOwner_PersistsPetAndRelationship()
    {
        var request = CreateValidRequest();
        request.Name = "  Buddy  ";
        request.Breed = "   ";

        var response = await service.CreateAsync(request);
        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Pets
            .AsNoTracking()
            .Include(pet => pet.Owner)
            .SingleAsync(pet => pet.Id == response.Id);

        Assert.That(response.Id, Is.GreaterThan(0));
        Assert.That(saved.Name, Is.EqualTo("Buddy"));
        Assert.That(saved.OwnerId, Is.EqualTo(ownerId));
        Assert.That(saved.Owner.FullName, Is.EqualTo("Pet Test Owner"));
        Assert.That(saved.Breed, Is.Null);
    }

    [Test]
    public async Task Create_WithMissingOwner_DoesNotSavePet()
    {
        var countBefore = await dbContext.Pets.CountAsync();
        var request = CreateValidRequest();
        request.OwnerId = int.MaxValue;

        var exception = Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await service.CreateAsync(request);
        });

        Assert.That(
            exception!.ValidationResult.MemberNames,
            Does.Contain(nameof(PetRequest.OwnerId)));
        Assert.That(
            await dbContext.Pets.CountAsync(),
            Is.EqualTo(countBefore));
    }

    [Test]
    public async Task GetById_WhenPetExists_ReturnsSavedPet()
    {
        var created = await service.CreateAsync(CreateValidRequest());
        dbContext.ChangeTracker.Clear();

        var response = await service.GetByIdAsync(created.Id);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Id, Is.EqualTo(created.Id));
        Assert.That(response.OwnerId, Is.EqualTo(ownerId));
        Assert.That(response.Name, Is.EqualTo("Buddy"));
    }

    [Test]
    public async Task GetAll_WithOwnerFilter_ExcludesOtherOwnersPets()
    {
        var created = await service.CreateAsync(CreateValidRequest());

        var otherOwner = new Owner
        {
            FullName = "Other Owner",
            Phone = "0400000001"
        };

        dbContext.Owners.Add(otherOwner);
        await dbContext.SaveChangesAsync();

        var otherRequest = CreateValidRequest();
        otherRequest.OwnerId = otherOwner.Id;
        otherRequest.Name = "Other Pet";
        await service.CreateAsync(otherRequest);

        dbContext.ChangeTracker.Clear();

        var responses = await service.GetAllAsync(ownerId);

        Assert.That(responses, Has.Count.EqualTo(1));
        Assert.That(responses[0].Id, Is.EqualTo(created.Id));
        Assert.That(responses[0].OwnerId, Is.EqualTo(ownerId));
    }

    [Test]
    public async Task Update_WhenPetExists_PersistsChanges()
    {
        var created = await service.CreateAsync(CreateValidRequest());
        dbContext.ChangeTracker.Clear();

        var request = CreateValidRequest();
        request.Name = "Updated Buddy";
        request.CareInstructions = null;

        var response = await service.UpdateAsync(created.Id, request);
        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Pets
            .AsNoTracking()
            .SingleAsync(pet => pet.Id == created.Id);

        Assert.That(response, Is.Not.Null);
        Assert.That(saved.Name, Is.EqualTo("Updated Buddy"));
        Assert.That(saved.CareInstructions, Is.Null);
        Assert.That(saved.OwnerId, Is.EqualTo(ownerId));
    }

    [Test]
    public async Task Update_WithMissingOwner_LeavesPetUnchanged()
    {
        var created = await service.CreateAsync(CreateValidRequest());
        dbContext.ChangeTracker.Clear();

        var request = CreateValidRequest();
        request.OwnerId = int.MaxValue;
        request.Name = "Should Not Be Saved";

        Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await service.UpdateAsync(created.Id, request);
        });

        dbContext.ChangeTracker.Clear();

        var saved = await dbContext.Pets
            .AsNoTracking()
            .SingleAsync(pet => pet.Id == created.Id);

        Assert.That(saved.Name, Is.EqualTo("Buddy"));
        Assert.That(saved.OwnerId, Is.EqualTo(ownerId));
    }

    [Test]
    public async Task Update_WhenPetDoesNotExist_ReturnsNull()
    {
        var response = await service.UpdateAsync(
            -1,
            CreateValidRequest());

        Assert.That(response, Is.Null);
    }

    private PetRequest CreateValidRequest()
    {
        return new PetRequest
        {
            OwnerId = ownerId,
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            CareInstructions = "Feed twice daily."
        };
    }
}