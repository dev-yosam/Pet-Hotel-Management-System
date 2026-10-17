using System.ComponentModel.DataAnnotations;
using PetHotel.Api.DTOs;

namespace PetHotel.Tests;

public class PetRequestTests
{
    [TestCase("123", false)]
    [TestCase("<script>", false)]
    [TestCase("R2D2", true)]
    [TestCase("Milo", true)]
    public void Name_Format(string name, bool valid)
    {
        var request = CreateValidRequest();
        request.Name = name;
        Assert.That(IsValid(request), Is.EqualTo(valid));
    }

    [TestCase("123", false)]
    [TestCase("Dog@", false)]
    [TestCase("Dog", true)]
    [TestCase("Guinea pig", true)]
    public void SpeciesAndBreed_Format(string text, bool valid)
    {
        var request = CreateValidRequest();
        request.Species = text;
        request.Breed = text;
        Assert.That(IsValid(request), Is.EqualTo(valid));
    }
    [TestCase(0, false)]
    [TestCase(-1, false)]
    [TestCase(1, true)]
    [TestCase(int.MaxValue, true)]
    public void OwnerId_ReturnsExpectedResult(int ownerId, bool expected)
    {
        var request = CreateValidRequest();
        request.OwnerId = ownerId;

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Name_WhenMissing_IsInvalid(string? name)
    {
        var request = CreateValidRequest();
        request.Name = name!;

        Assert.That(IsValid(request), Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Species_WhenMissing_IsInvalid(string? species)
    {
        var request = CreateValidRequest();
        request.Species = species!;

        Assert.That(IsValid(request), Is.False);
    }

    [TestCase(100, true)]
    [TestCase(101, false)]
    public void Name_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.Name = new string('A', length);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(50, true)]
    [TestCase(51, false)]
    public void Species_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.Species = new string('A', length);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(100, true)]
    [TestCase(101, false)]
    public void Breed_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.Breed = new string('A', length);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [TestCase(1000, true)]
    [TestCase(1001, false)]
    public void CareInstructions_LengthBoundary_ReturnsExpectedResult(
        int length,
        bool expected)
    {
        var request = CreateValidRequest();
        request.CareInstructions = new string('A', length);

        Assert.That(IsValid(request), Is.EqualTo(expected));
    }

    [Test]
    public void OptionalFields_WhenNull_IsValid()
    {
        var request = CreateValidRequest();
        request.Breed = null;
        request.CareInstructions = null;

        Assert.That(IsValid(request), Is.True);
    }

    private static PetRequest CreateValidRequest()
    {
        return new PetRequest
        {
            OwnerId = 1,
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever",
            CareInstructions = "Feed twice daily."
        };
    }

    private static bool IsValid(PetRequest request)
    {
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            new List<ValidationResult>(),
            validateAllProperties: true);
    }
}
