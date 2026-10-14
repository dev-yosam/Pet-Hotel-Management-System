using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IBookingService
{
    Task<BookingResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<BookingResponse> CreateAsync(
        BookingRequest request,
        CancellationToken cancellationToken = default);
}