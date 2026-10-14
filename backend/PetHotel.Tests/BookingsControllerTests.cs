using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.Controllers;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Tests;

public class BookingsControllerTests
{
    [Test]
    public async Task GetById_WhenBookingExists_ReturnsBooking()
    {
        var service = new StubBookingService();
        var controller = new BookingsController(service);

        var response = await controller.GetById(
            service.Booking.Id,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.SameAs(service.Booking));
            Assert.That(service.ReceivedId, Is.EqualTo(service.Booking.Id));
        });
    }

    [Test]
    public async Task GetById_WhenBookingDoesNotExist_ReturnsNotFound()
    {
        var service = new StubBookingService();
        var controller = new BookingsController(service);

        var response = await controller.GetById(
            999999,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var service = new StubBookingService();
        var controller = new BookingsController(service);
        var request = new BookingRequest
        {
            PetId = 1,
            RoomId = 2,
            ScheduledCheckIn = service.Booking.ScheduledCheckIn,
            ScheduledCheckOut = service.Booking.ScheduledCheckOut
        };

        var response = await controller.Create(
            request,
            CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(
                result.ActionName,
                Is.EqualTo(nameof(BookingsController.GetById)));
            Assert.That(
                result.RouteValues!["id"],
                Is.EqualTo(service.Booking.Id));
            Assert.That(result.Value, Is.SameAs(service.Booking));
            Assert.That(service.ReceivedRequest, Is.SameAs(request));
        });
    }

    private sealed class StubBookingService : IBookingService
    {
        public BookingResponse Booking { get; } = new()
        {
            Id = 10,
            PetId = 1,
            RoomId = 2,
            ScheduledCheckIn =
                new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
            ScheduledCheckOut =
                new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero),
            Status = "Booked"
        };

        public int? ReceivedId { get; private set; }
        public BookingRequest? ReceivedRequest { get; private set; }

        public Task<BookingResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            ReceivedId = id;

            return Task.FromResult<BookingResponse?>(
                id == Booking.Id ? Booking : null);
        }

        public Task<BookingResponse> CreateAsync(
            BookingRequest request,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            return Task.FromResult(Booking);
        }
    }
}
