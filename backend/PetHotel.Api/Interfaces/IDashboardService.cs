using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
