namespace PetHotel.Api.Models;

public class Owner
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
