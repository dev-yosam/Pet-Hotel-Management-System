namespace PetHotel.Api.Models;

public class Stay
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int RoomId { get; set; }
    public DateTimeOffset ScheduledCheckIn { get; set; }
    public DateTimeOffset ScheduledCheckOut { get; set; }
    public DateTimeOffset? ActualCheckIn { get; set; }
    public DateTimeOffset? ActualCheckOut { get; set; }
    public StayStatus Status { get; set; } = StayStatus.Booked;
    public Pet Pet { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<CareRecord> CareRecords { get; set; } = new List<CareRecord>();
}
