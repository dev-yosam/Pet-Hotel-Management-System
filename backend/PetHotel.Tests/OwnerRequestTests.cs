using System.ComponentModel.DataAnnotations;
using PetHotel.Api.DTOs;

namespace PetHotel.Tests;

public class OwnerRequestTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void FullName_WhenMissing_IsInvalid(string? fullName)
    {
        var request = CreateValidRequest();
        request.FullName = fullName!;

        Assert.That(IsValid(request), Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Phone_WhenMissing_IsInvalid(string? phone)
    {
        var request = CreateValidRequest();
        request.Phone = phone!;

        Assert.That(IsValid(request), Is.False);
    }

    [TestCase(100, true)]
    [TestCase(101, false)]
    public void FullName_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.FullName = new string('A', length);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(30, false)]
    [TestCase(31, false)]
    public void Phone_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.Phone = new string('1', length);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(null, true)]
    [TestCase("alan@example.com", true)]
    [TestCase("invalid-email", false)]
    public void Email_Format_ReturnsExpectedResult(
        string? email,
        bool expected)
    {
        var request = CreateValidRequest();
        request.Email = email;

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(254, true)]
    [TestCase(255, false)]
    public void Email_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.Email = new string('a', 64) + "@" + new string('b', 63) + "." +
            new string('c', 63) + "." + new string('d', length - 197) + ".com";

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    private static OwnerRequest CreateValidRequest()
    {
        return new OwnerRequest
        {
            FullName = "Alan Chang",
            Phone = "0412345678",
            Email = "alan@example.com"
        };
    }

    private static bool IsValid(OwnerRequest request)
    {
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            new List<ValidationResult>(),
            validateAllProperties: true);
    }
}
