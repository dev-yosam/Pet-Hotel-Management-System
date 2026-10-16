using System.ComponentModel.DataAnnotations;
using PetHotel.Api.DTOs;

namespace PetHotel.Tests;

public class OwnerRequestTests
{
    [TestCase("2123", false)]
    [TestCase("Sam123", false)]
    [TestCase("---", false)]
    [TestCase("Sam@", false)]
    [TestCase("Anne-Marie O'Neill", true)]
    [TestCase("José Lee", true)]
    [TestCase("王小明", true)]
    public void FullName_Format_ReturnsExpectedResult(string name, bool expected)
    {
        var request = CreateValidRequest();
        request.FullName = name;
        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase("rewrew", false)]
    [TestCase("0412abc678", false)]
    [TestCase("+ ()-", false)]
    [TestCase("04+12345678", false)]
    [TestCase("0412345678", true)]
    [TestCase("+61 412 345 678", true)]
    [TestCase("(02) 1234-5678", true)]
    public void Phone_Format_ReturnsExpectedResult(string phone, bool expected)
    {
        var request = CreateValidRequest();
        request.Phone = phone;
        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

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

    [TestCase(30, true)]
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
        request.Email = new string('a', length - 12) + "@example.com";

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
