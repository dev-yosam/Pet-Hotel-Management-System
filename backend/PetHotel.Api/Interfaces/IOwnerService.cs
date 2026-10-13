using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IOwnerService
{
    Task<List<OwnerResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<OwnerResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OwnerResponse> CreateAsync(
        OwnerRequest request,
        CancellationToken cancellationToken = default);

    Task<OwnerResponse?> UpdateAsync(
        int id,
        OwnerRequest request,
        CancellationToken cancellationToken = default);
}