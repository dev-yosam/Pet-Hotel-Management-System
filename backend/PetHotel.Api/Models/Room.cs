namespace PetHotel.Api.Models;

public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Stay> Stays { get; set; } = new List<Stay>();
}
