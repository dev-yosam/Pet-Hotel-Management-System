namespace PetHotel.Api.DTOs;

public class StayResponse
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int RoomId { get; set; }
    public DateTimeOffset ScheduledCheckIn { get; set; }
    public DateTimeOffset ScheduledCheckOut { get; set; }
    public DateTimeOffset? ActualCheckIn { get; set; }
    public DateTimeOffset? ActualCheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalCharge { get; set; }
}