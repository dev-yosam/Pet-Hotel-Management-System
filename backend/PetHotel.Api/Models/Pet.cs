namespace PetHotel.Api.Models;

public class Pet
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string? CareInstructions { get; set; }
    public Owner Owner { get; set; } = null!;
    public ICollection<Stay> Stays { get; set; } = new List<Stay>();
}
