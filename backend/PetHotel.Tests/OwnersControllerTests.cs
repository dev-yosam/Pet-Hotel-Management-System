using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.Controllers;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Tests;

public class OwnersControllerTests
{
    [Test]
    public async Task GetById_WhenOwnerExists_ReturnsOwner()
    {
        var owner = CreateOwnerResponse();
        var service = new StubOwnerService
        {
            GetByIdResult = owner
        };
        var controller = new OwnersController(service);

        var response = await controller.GetById(
            owner.Id,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;

        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Value, Is.SameAs(owner));
    }

    [Test]
    public async Task GetById_WhenOwnerDoesNotExist_ReturnsNotFound()
    {
        var controller = new OwnersController(new StubOwnerService());

        var response = await controller.GetById(
            999,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_WhenSuccessful_ReturnsCreatedAtGetById()
    {
        var owner = CreateOwnerResponse();
        var service = new StubOwnerService
        {
            CreateResult = owner
        };
        var controller = new OwnersController(service);
        var request = new OwnerRequest
        {
            FullName = owner.FullName,
            Phone = owner.Phone,
            Email = owner.Email
        };

        var response = await controller.Create(
            request,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        Assert.That(result.StatusCode, Is.EqualTo(201));
        Assert.That(result.ActionName, Is.EqualTo(nameof(OwnersController.GetById)));
        Assert.That(result.RouteValues!["id"], Is.EqualTo(owner.Id));
        Assert.That(result.Value, Is.SameAs(owner));
        Assert.That(service.LastCreateRequest, Is.SameAs(request));
    }

    [Test]
    public async Task Update_WhenOwnerDoesNotExist_ReturnsNotFound()
    {
        var controller = new OwnersController(new StubOwnerService());
        var request = new OwnerRequest
        {
            FullName = "Demo Owner",
            Phone = "0400000000"
        };

        var response = await controller.Update(
            999,
            request,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<NotFoundResult>());
    }

    private static OwnerResponse CreateOwnerResponse()
    {
        return new OwnerResponse
        {
            Id = 1,
            FullName = "Demo Owner",
            Phone = "0400000000",
            Email = "demo@example.com"
        };
    }

    private sealed class StubOwnerService : IOwnerService
    {
        public OwnerResponse? GetByIdResult { get; init; }
        public OwnerResponse? CreateResult { get; init; }
        public OwnerRequest? LastCreateRequest { get; private set; }

        public Task<List<OwnerResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OwnerResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIdResult);
        }

        public Task<OwnerResponse> CreateAsync(
            OwnerRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCreateRequest = request;

            return Task.FromResult(
                CreateResult ?? throw new InvalidOperationException(
                    "CreateResult must be configured for this test."));
        }

        public Task<OwnerResponse?> UpdateAsync(
            int id,
            OwnerRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OwnerResponse?>(null);
        }
    }
}