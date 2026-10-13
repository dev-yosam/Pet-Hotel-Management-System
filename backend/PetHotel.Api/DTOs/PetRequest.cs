using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class PetRequest
{
    [Range(1, int.MaxValue)]
    public int OwnerId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Species { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Breed { get; set; }

    [StringLength(1000)]
    public string? CareInstructions { get; set; }
}