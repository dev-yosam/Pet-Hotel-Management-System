using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.Controllers;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Tests;

public class RoomsControllerTests
{
    [Test]
    public async Task GetAll_ReturnsRooms()
    {
        var service = new StubRoomService();
        var controller = new RoomsController(service);

        var response = await controller.GetAll(CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;
        Assert.That(result.Value, Is.SameAs(service.Rooms));
    }

    [Test]
    public async Task GetAvailable_WithValidDates_ReturnsRooms()
    {
        var service = new StubRoomService();
        var controller = new RoomsController(service);
        var checkIn = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var checkOut = checkIn.AddDays(2);

        var response = await controller.GetAvailable(
            checkIn, checkOut, CancellationToken.None);

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.SameAs(service.Rooms));
            Assert.That(service.ReceivedCheckIn, Is.EqualTo(checkIn));
            Assert.That(service.ReceivedCheckOut, Is.EqualTo(checkOut));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task GetAvailable_WithInvalidDates_ReturnsBadRequest(
        int stayDays)
    {
        var service = new StubRoomService();
        var controller = new RoomsController(service);
        var checkIn = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        var response = await controller.GetAvailable(
            checkIn, checkIn.AddDays(stayDays), CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<ObjectResult>());
        var result = (ObjectResult)response.Result!;

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(controller.ModelState.ContainsKey("checkOut"), Is.True);
            Assert.That(service.ReceivedCheckIn, Is.Null);
        });
    }

    private sealed class StubRoomService : IRoomService
    {
        public List<RoomResponse> Rooms { get; } =
        [
            new RoomResponse
            {
                Id = 1,
                RoomNumber = "A101",
                RoomType = "Standard",
                IsActive = true
            }
        ];

        public DateTimeOffset? ReceivedCheckIn { get; private set; }
        public DateTimeOffset? ReceivedCheckOut { get; private set; }

        public Task<List<RoomResponse>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Rooms);
        }

        public Task<List<RoomResponse>> GetAvailableAsync(
            DateTimeOffset checkIn,
            DateTimeOffset checkOut,
            CancellationToken cancellationToken = default)
        {
            ReceivedCheckIn = checkIn;
            ReceivedCheckOut = checkOut;
            return Task.FromResult(Rooms);
        }
    }
}
