using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IRoomService
{
    Task<List<RoomResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<List<RoomResponse>> GetAvailableAsync(
        DateTimeOffset checkIn,
        DateTimeOffset checkOut,
        CancellationToken cancellationToken = default);
}