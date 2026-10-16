using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class OwnerRequest
{
    [Required]
    [StringLength(100)]
    [RegularExpression(@"(?=.*\p{L})[\p{L}\p{M} .'’\-]+",
        ErrorMessage = "Name must contain letters and cannot contain numbers or other symbols.")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [RegularExpression(@"(?=.*[0-9]) *\+?[0-9 ()\-]+",
        ErrorMessage = "Phone must contain digits and may include a leading +, spaces, parentheses or hyphens.")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }
}
