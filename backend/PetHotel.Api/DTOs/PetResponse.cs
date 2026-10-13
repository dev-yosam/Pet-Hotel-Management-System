namespace PetHotel.Api.DTOs;

public class PetResponse
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string? CareInstructions { get; set; }
}