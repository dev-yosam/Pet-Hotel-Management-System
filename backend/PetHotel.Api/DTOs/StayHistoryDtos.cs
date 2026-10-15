using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.DTOs;

public class StayHistoryQuery : IValidatableObject
{
    [StringLength(100)]
    public string? Pet { get; set; }
    [StringLength(100)]
    public string? Owner { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [RegularExpression("^(Booked|CheckedIn|CheckedOut|Cancelled)$")]
    public string? Status { get; set; }
    [RegularExpression("^(newest|oldest|pet|owner)$")]
    public string Sort { get; set; } = "newest";
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From > To)
            yield return new ValidationResult("End date must be on or after start date.", [nameof(To)]);
        if (From?.Year > 9998 || To?.Year > 9998)
            yield return new ValidationResult("Choose a date before year 9999.", [nameof(From), nameof(To)]);
    }
}

public record StayHistoryItem(
    int Id, string PetName, string Species, string OwnerName, string OwnerPhone,
    string RoomNumber, string RoomType, string Status,
    DateTimeOffset ScheduledCheckIn, DateTimeOffset ScheduledCheckOut,
    DateTimeOffset? ActualCheckIn, DateTimeOffset? ActualCheckOut, decimal? TotalCharge);

public record StayHistoryResult(List<StayHistoryItem> Items, int TotalCount, int Page, int PageSize);
