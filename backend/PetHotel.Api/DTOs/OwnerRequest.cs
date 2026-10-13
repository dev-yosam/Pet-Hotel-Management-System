using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class OwnerRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }
}