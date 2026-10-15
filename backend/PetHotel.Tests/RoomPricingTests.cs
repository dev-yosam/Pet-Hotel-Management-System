using PetHotel.Api.Services.Pricing;

namespace PetHotel.Tests;

public class RoomPricingTests
{
    [TestCase(1, 40)]
    [TestCase(2, 80)]
    [TestCase(7, 280)]
    public void Standard_CalculatesCharge(int nights, int expectedAmount)
    {
        RoomPricing pricing = new StandardRoomPricing();

        var amount = pricing.CalculateCharge(nights);

        Assert.That(amount, Is.EqualTo((decimal)expectedAmount));
    }

    [TestCase(1, 60)]
    [TestCase(6, 360)]
    [TestCase(7, 378)]
    [TestCase(8, 432)]
    public void Deluxe_AppliesDiscountFromSevenNights(
        int nights,
        int expectedAmount)
    {
        RoomPricing pricing = new DeluxeRoomPricing();

        var amount = pricing.CalculateCharge(nights);

        Assert.That(amount, Is.EqualTo((decimal)expectedAmount));
    }

    [TestCase("Standard", 0)]
    [TestCase("Standard", -1)]
    [TestCase("Deluxe", 0)]
    [TestCase("Deluxe", -1)]
    public void CalculateCharge_WithInvalidNights_Throws(
        string roomType,
        int nights)
    {
        RoomPricing pricing = roomType == "Standard"
            ? new StandardRoomPricing()
            : new DeluxeRoomPricing();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            pricing.CalculateCharge(nights);
        });

        Assert.That(exception!.ParamName, Is.EqualTo("nights"));
    }
}