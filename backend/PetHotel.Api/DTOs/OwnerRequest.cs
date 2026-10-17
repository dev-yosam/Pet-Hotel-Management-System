using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class OwnerRequest
{
    [Required]
    [StringLength(100)]
    [RegularExpression(InputRules.PersonName,
        ErrorMessage = "Name must contain letters and cannot contain numbers or other symbols.")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    [RegularExpression(InputRules.Phone,
        ErrorMessage = "Phone must contain 7 to 15 digits. Use a leading +, single separators and matching parentheses.")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [RegularExpression(InputRules.Email, ErrorMessage = "Enter an email such as name@example.com, without spaces.")]
    [StringLength(254)]
    public string? Email { get; set; }
}
