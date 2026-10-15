using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IStayService
{
    Task<StayResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<StayResponse> CheckInAsync(
        int id,
        CancellationToken cancellationToken = default);
}