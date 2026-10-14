namespace PetHotel.Api.DTOs;

public class BookingResponse
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int RoomId { get; set; }
    public DateTimeOffset ScheduledCheckIn { get; set; }
    public DateTimeOffset ScheduledCheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
}