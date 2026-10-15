namespace PetHotel.Api.Services.Pricing;

public sealed class DeluxeRoomPricing : RoomPricing
{
    public override string RoomType => "Deluxe";

    protected override decimal CalculateAmount(int nights)
    {
        var amount = nights * 60m;

        return nights >= 7 ? amount * 0.9m : amount;
    }
}