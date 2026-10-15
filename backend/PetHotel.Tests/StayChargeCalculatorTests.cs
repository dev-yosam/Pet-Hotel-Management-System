using System.Globalization;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Services;
using PetHotel.Api.Services.Pricing;

namespace PetHotel.Tests;

public class StayChargeCalculatorTests
{
    private IStayChargeCalculator calculator = null!;

    [SetUp]
    public void SetUp()
    {
        calculator = new StayChargeCalculator(
        [
            new StandardRoomPricing(),
            new DeluxeRoomPricing()
        ]);
    }

    [TestCase("Standard", 80)]
    [TestCase("Deluxe", 120)]
    [TestCase("standard", 80)]
    public void CalculateCharge_UsesMatchingRoomPricing(
        string roomType,
        int expectedAmount)
    {
        var amount = calculator.CalculateCharge(
            roomType,
            Parse("2026-11-10T10:00:00+11:00"),
            Parse("2026-11-12T10:00:00+11:00"));

        Assert.That(amount, Is.EqualTo((decimal)expectedAmount));
    }

    [Test]
    public void CalculateCharge_ForSevenDeluxeNights_AppliesDiscount()
    {
        var amount = calculator.CalculateCharge(
            "Deluxe",
            Parse("2026-11-10T10:00:00+11:00"),
            Parse("2026-11-17T10:00:00+11:00"));

        Assert.That(amount, Is.EqualTo(378m));
    }

    [Test]
    public void CalculateCharge_ForSameDay_ChargesOneNight()
    {
        var amount = calculator.CalculateCharge(
            "Standard",
            Parse("2026-11-10T10:00:00+11:00"),
            Parse("2026-11-10T15:00:00+11:00"));

        Assert.That(amount, Is.EqualTo(40m));
    }

    [Test]
    public void CalculateCharge_UsesSydneyDatesInsteadOfUtcDates()
    {
        var amount = calculator.CalculateCharge(
            "Standard",
            Parse("2026-11-10T12:30:00Z"),
            Parse("2026-11-11T13:30:00Z"));

        // Sydney: 10 November 23:30 to 12 November 00:30.
        Assert.That(amount, Is.EqualTo(80m));
    }

    [TestCase(
        "2026-10-03T10:00:00+10:00",
        "2026-10-04T10:00:00+11:00",
        23)]
    [TestCase(
        "2026-04-04T10:00:00+11:00",
        "2026-04-05T10:00:00+10:00",
        25)]
    public void CalculateCharge_AcrossDaylightSaving_ChargesOneNight(
        string start,
        string end,
        int elapsedHours)
    {
        var checkIn = Parse(start);
        var checkOut = Parse(end);

        var amount = calculator.CalculateCharge(
            "Standard",
            checkIn,
            checkOut);

        Assert.Multiple(() =>
        {
            Assert.That(
                (checkOut - checkIn).TotalHours,
                Is.EqualTo(elapsedHours));
            Assert.That(amount, Is.EqualTo(40m));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CalculateCharge_WithInvalidDates_ThrowsInvalidInput(
        int hours)
    {
        var checkIn = Parse("2026-11-10T10:00:00+11:00");

        var exception = Assert.Throws<StayException>(() =>
        {
            calculator.CalculateCharge(
                "Standard",
                checkIn,
                checkIn.AddHours(hours));
        });

        Assert.That(exception!.Error, Is.EqualTo(StayError.InvalidInput));
    }

    [Test]
    public void CalculateCharge_WithUnknownRoomType_ThrowsInvalidInput()
    {
        var exception = Assert.Throws<StayException>(() =>
        {
            calculator.CalculateCharge(
                "Unknown",
                Parse("2026-11-10T10:00:00+11:00"),
                Parse("2026-11-12T10:00:00+11:00"));
        });

        Assert.That(exception!.Error, Is.EqualTo(StayError.InvalidInput));
    }

    private static DateTimeOffset Parse(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }
}