using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Services.Pricing;

public class StayChargeCalculator(
    IEnumerable<RoomPricing> pricingRules) : IStayChargeCalculator
{
    private static readonly TimeZoneInfo SydneyTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    private readonly List<RoomPricing> rules = pricingRules.ToList();

    public decimal CalculateCharge(
        string roomType,
        DateTimeOffset scheduledCheckIn,
        DateTimeOffset scheduledCheckOut)
    {
        if (scheduledCheckOut <= scheduledCheckIn)
        {
            throw new StayException(
                StayError.InvalidInput,
                "Check-out must be after check-in.");
        }

        var pricing = rules.SingleOrDefault(rule =>
            string.Equals(
                rule.RoomType,
                roomType,
                StringComparison.OrdinalIgnoreCase));

        if (pricing is null)
        {
            throw new StayException(
                StayError.InvalidInput,
                "The room type does not have a pricing rule.");
        }

        var localCheckIn = TimeZoneInfo.ConvertTime(
            scheduledCheckIn,
            SydneyTimeZone);

        var localCheckOut = TimeZoneInfo.ConvertTime(
            scheduledCheckOut,
            SydneyTimeZone);

        var startDate = DateOnly.FromDateTime(localCheckIn.DateTime);
        var endDate = DateOnly.FromDateTime(localCheckOut.DateTime);
        var nights = Math.Max(1, endDate.DayNumber - startDate.DayNumber);

        return pricing.CalculateCharge(nights);
    }
}