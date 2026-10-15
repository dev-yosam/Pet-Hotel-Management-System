namespace PetHotel.Api.Services.Pricing;

public abstract class RoomPricing
{
    public abstract string RoomType { get; }

    public decimal CalculateCharge(int nights)
    {
        if (nights < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nights),
                "At least one night is required.");
        }

        return decimal.Round(
            CalculateAmount(nights),
            2,
            MidpointRounding.AwayFromZero);
    }

    protected abstract decimal CalculateAmount(int nights);
}