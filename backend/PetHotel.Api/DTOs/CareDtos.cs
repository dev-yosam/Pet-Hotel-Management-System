using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public record CareTaskDto(int Id, int StayId, string Description, DateTimeOffset ScheduledAt,
    bool IsCompleted, DateTimeOffset? CompletedAt);

public record CareStayDto(int Id, string PetName, string OwnerName, string RoomNumber,
    string Status, DateTimeOffset? ActualCheckIn, DateTimeOffset ScheduledCheckOut, string? CareInstructions);

public class CreateCareTaskRequest
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset? ScheduledAt { get; set; }
}

public class UpdateCareCompletionRequest
{
    [Required]
    public bool? IsCompleted { get; set; }
}
