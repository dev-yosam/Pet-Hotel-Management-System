namespace PetHotel.Api.DTOs;

public class RoomResponse
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}