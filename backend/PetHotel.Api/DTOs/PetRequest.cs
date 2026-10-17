using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class PetRequest
{
    [Range(1, int.MaxValue)]
    public int OwnerId { get; set; }

    [Required]
    [StringLength(100)]
    [RegularExpression(InputRules.PetName, ErrorMessage = "Pet name must include a letter. Use letters, digits, spaces, periods, apostrophes or hyphens.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [RegularExpression(InputRules.AnimalType, ErrorMessage = "Species must contain letters, not numbers or other symbols.")]
    public string Species { get; set; } = string.Empty;

    [StringLength(100)]
    [RegularExpression("^(?:" + InputRules.AnimalType + "| *)$", ErrorMessage = "Breed must contain letters, not numbers or other symbols.")]
    public string? Breed { get; set; }

    [StringLength(1000)]
    public string? CareInstructions { get; set; }
}
