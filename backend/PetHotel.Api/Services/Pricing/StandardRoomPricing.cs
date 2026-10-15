namespace PetHotel.Api.Services.Pricing;

public sealed class StandardRoomPricing : RoomPricing
{
    public override string RoomType => "Standard";

    protected override decimal CalculateAmount(int nights)
    {
        return nights * 40m;
    }
}