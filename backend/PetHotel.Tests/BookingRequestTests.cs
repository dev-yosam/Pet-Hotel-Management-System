using System.ComponentModel.DataAnnotations;
using PetHotel.Api.DTOs;

namespace PetHotel.Tests;

public class BookingRequestTests
{
    [TestCase(0, false)]
    [TestCase(-1, false)]
    [TestCase(1, true)]
    public void PetId_ReturnsExpectedResult(int petId, bool expected)
    {
        var request = CreateValidRequest();
        request.PetId = petId;

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(0, false)]
    [TestCase(-1, false)]
    [TestCase(1, true)]
    public void RoomId_ReturnsExpectedResult(int roomId, bool expected)
    {
        var request = CreateValidRequest();
        request.RoomId = roomId;

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [Test]
    public void CheckIn_WhenMissing_IsInvalid()
    {
        var request = CreateValidRequest();
        request.ScheduledCheckIn = null;

        Assert.That(IsValid(request), Is.False);
    }

    [Test]
    public void CheckOut_WhenMissing_IsInvalid()
    {
        var request = CreateValidRequest();
        request.ScheduledCheckOut = null;

        Assert.That(IsValid(request), Is.False);
    }

    [TestCase(-1, false)]
    [TestCase(0, false)]
    [TestCase(1, true)]
    public void CheckOut_RelativeToCheckIn_ReturnsExpectedResult(
        int minutes,
        bool expected)
    {
        var request = CreateValidRequest();
        request.ScheduledCheckOut =
            request.ScheduledCheckIn!.Value.AddMinutes(minutes);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [Test]
    public void CheckOut_AtSameInstantWithDifferentOffset_IsInvalid()
    {
        var request = CreateValidRequest();
        request.ScheduledCheckIn =
            new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(11));
        request.ScheduledCheckOut =
            new DateTimeOffset(2026, 10, 31, 23, 0, 0, TimeSpan.Zero);

        Assert.That(IsValid(request), Is.False);
    }

    [Test]
    public void InvalidDateOrder_IdentifiesCheckOutField()
    {
        var request = CreateValidRequest();
        request.ScheduledCheckOut = request.ScheduledCheckIn;
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.That(valid, Is.False);
        Assert.That(
            results.SelectMany(result => result.MemberNames),
            Does.Contain(nameof(BookingRequest.ScheduledCheckOut)));
    }

    private static BookingRequest CreateValidRequest()
    {
        return new BookingRequest
        {
            PetId = 1,
            RoomId = 1,
            ScheduledCheckIn =
                new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            ScheduledCheckOut =
                new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero)
        };
    }

    private static bool IsValid(BookingRequest request)
    {
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            new List<ValidationResult>(),
            validateAllProperties: true);
    }
}