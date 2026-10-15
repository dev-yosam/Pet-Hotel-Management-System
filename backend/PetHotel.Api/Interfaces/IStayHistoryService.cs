using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IStayHistoryService
{
    Task<StayHistoryResult> SearchAsync(StayHistoryQuery query, CancellationToken cancellationToken = default);
}
