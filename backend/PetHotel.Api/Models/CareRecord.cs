namespace PetHotel.Api.Models;

public class CareRecord
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Stay Stay { get; set; } = null!;
}
