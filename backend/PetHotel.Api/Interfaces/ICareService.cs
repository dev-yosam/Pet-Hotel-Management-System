using PetHotel.Api.DTOs;

namespace PetHotel.Api.Interfaces;

public interface ICareService
{
    Task<List<CareStayDto>> GetStaysAsync(CancellationToken cancellationToken = default);
    Task<List<CareTaskDto>> GetTasksAsync(int stayId, CancellationToken cancellationToken = default);
    Task<CareTaskDto> GetTaskAsync(int taskId, CancellationToken cancellationToken = default);
    Task<CareTaskDto> CreateTaskAsync(int stayId, CreateCareTaskRequest request, CancellationToken cancellationToken = default);
    Task<CareTaskDto> UpdateCompletionAsync(int taskId, bool isCompleted, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default);
}
