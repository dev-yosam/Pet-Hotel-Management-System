using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class BookingRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int PetId { get; set; }

    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    [Required]
    public DateTimeOffset? ScheduledCheckIn { get; set; }

    [Required]
    public DateTimeOffset? ScheduledCheckOut { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (ScheduledCheckIn.HasValue &&
            ScheduledCheckOut.HasValue &&
            ScheduledCheckOut.Value <= ScheduledCheckIn.Value)
        {
            yield return new ValidationResult(
                "Check-out must be after check-in.",
                new[] { nameof(ScheduledCheckOut) });
        }
    }
}