using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.Controllers;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Tests;

public class StaysControllerTests
{
    [Test]
    public async Task GetById_WhenStayExists_ReturnsStay()
    {
        var service = new StubStayService();
        var controller = new StaysController(service);

        var response = await controller.GetById(
            service.Stay.Id,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.SameAs(service.Stay));
            Assert.That(service.ReceivedId, Is.EqualTo(service.Stay.Id));
        });
    }

    [Test]
    public async Task GetById_WhenStayDoesNotExist_ReturnsNotFound()
    {
        var service = new StubStayService();
        var controller = new StaysController(service);

        var response = await controller.GetById(
            999999,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CheckIn_ReturnsUpdatedStay()
    {
        var service = new StubStayService();
        var controller = new StaysController(service);

        var response = await controller.CheckIn(
            service.Stay.Id,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.SameAs(service.Stay));
            Assert.That(service.ReceivedId, Is.EqualTo(service.Stay.Id));
            Assert.That(service.Stay.Status, Is.EqualTo("CheckedIn"));
            Assert.That(
                service.Stay.ActualCheckIn,
                Is.EqualTo(StubStayService.CheckInTime));
        });
    }

    [Test]
    public async Task CheckOut_ReturnsSettledStay()
    {
        var service = new StubStayService();
        var controller = new StaysController(service);

        var response = await controller.CheckOut(
            service.Stay.Id,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Value, Is.SameAs(service.Stay));
            Assert.That(service.ReceivedId, Is.EqualTo(service.Stay.Id));
            Assert.That(service.Stay.Status, Is.EqualTo("CheckedOut"));
            Assert.That(
                service.Stay.ActualCheckOut,
                Is.EqualTo(StubStayService.CheckInTime.AddDays(1)));
            Assert.That(service.Stay.TotalCharge, Is.EqualTo(80m));
        });
    }

    private sealed class StubStayService : IStayService
    {
        public static readonly DateTimeOffset CheckInTime =
            new(2026, 11, 10, 2, 0, 0, TimeSpan.Zero);

        public StayResponse Stay { get; } = new()
        {
            Id = 10,
            PetId = 1,
            RoomId = 2,
            ScheduledCheckIn = CheckInTime.AddHours(-1),
            ScheduledCheckOut = CheckInTime.AddDays(2),
            Status = "Booked"
        };

        public int? ReceivedId { get; private set; }

        public Task<StayResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            ReceivedId = id;

            return Task.FromResult<StayResponse?>(
                id == Stay.Id ? Stay : null);
        }

        public Task<StayResponse> CheckInAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            ReceivedId = id;
            Stay.Status = "CheckedIn";
            Stay.ActualCheckIn = CheckInTime;

            return Task.FromResult(Stay);
        }
        public Task<StayResponse> CheckOutAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            ReceivedId = id;
            Stay.Status = "CheckedOut";
            Stay.ActualCheckOut = CheckInTime.AddDays(1);
            Stay.TotalCharge = 80m;

            return Task.FromResult(Stay);
        }
    }
}