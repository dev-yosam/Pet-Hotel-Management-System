using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PetHotel.Api.Controllers;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Tests;

public class PetsControllerTests
{
    private ServiceProvider serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();

        serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        serviceProvider.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Save_WhenOwnerDoesNotExist_ReturnsValidationProblem(
        bool isUpdate)
    {
        var controller = CreateController(new StubPetService
        {
            RejectOwner = true
        });
        var request = CreatePetRequest();

        var response = isUpdate
            ? await controller.Update(1, request, CancellationToken.None)
            : await controller.Create(request, CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<ObjectResult>());
        var result = (ObjectResult)response.Result!;

        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Value, Is.TypeOf<ValidationProblemDetails>());
        var problem = (ValidationProblemDetails)result.Value!;

        Assert.That(problem.Errors.ContainsKey("OwnerId"), Is.True);
        Assert.That(
            problem.Errors["OwnerId"],
            Does.Contain("The selected owner does not exist."));
    }

    [Test]
    public async Task GetById_WhenPetDoesNotExist_ReturnsNotFound()
    {
        var controller = CreateController(new StubPetService());

        var response = await controller.GetById(
            999,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Update_WhenPetDoesNotExist_ReturnsNotFound()
    {
        var controller = CreateController(new StubPetService());

        var response = await controller.Update(
            999,
            CreatePetRequest(),
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<NotFoundResult>());
    }

    private PetsController CreateController(IPetService service)
    {
        return new PetsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProvider
                }
            }
        };
    }

    private static PetRequest CreatePetRequest()
    {
        return new PetRequest
        {
            OwnerId = 999,
            Name = "Buddy",
            Species = "Dog"
        };
    }

    private sealed class StubPetService : IPetService
    {
        public bool RejectOwner { get; init; }

        public Task<List<PetResponse>> GetAllAsync(
            int? ownerId = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PetResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PetResponse?>(null);
        }

        public Task<PetResponse> CreateAsync(
            PetRequest request,
            CancellationToken cancellationToken = default)
        {
            if (RejectOwner)
            {
                throw CreateOwnerException(request.OwnerId);
            }

            throw new NotSupportedException();
        }

        public Task<PetResponse?> UpdateAsync(
            int id,
            PetRequest request,
            CancellationToken cancellationToken = default)
        {
            if (RejectOwner)
            {
                throw CreateOwnerException(request.OwnerId);
            }

            return Task.FromResult<PetResponse?>(null);
        }

        private static ValidationException CreateOwnerException(int ownerId)
        {
            return new ValidationException(
                new ValidationResult(
                    "The selected owner does not exist.",
                    new[] { nameof(PetRequest.OwnerId) }),
                null,
                ownerId);
        }
    }
}