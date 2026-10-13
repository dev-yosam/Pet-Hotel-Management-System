using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IPetService
{
    Task<List<PetResponse>> GetAllAsync(
        int? ownerId = null,
        CancellationToken cancellationToken = default);

    Task<PetResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PetResponse> CreateAsync(
        PetRequest request,
        CancellationToken cancellationToken = default);

    Task<PetResponse?> UpdateAsync(
        int id,
        PetRequest request,
        CancellationToken cancellationToken = default);
}