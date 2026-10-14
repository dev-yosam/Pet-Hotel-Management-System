using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

public class BookingExceptionHandlerTests
{
    [TestCase(BookingError.InvalidInput, 400)]
    [TestCase(BookingError.NotFound, 404)]
    [TestCase(BookingError.Conflict, 409)]
    public async Task HandleBookingException_ReturnsExpectedProblem(
        BookingError error,
        int expectedStatus)
    {
        await AssertProblemAsync(
            new BookingException(error, "Booking test message."),
            expectedStatus,
            "Booking test message.");
    }

    [TestCase(CareError.InvalidInput, 400)]
    [TestCase(CareError.NotFound, 404)]
    [TestCase(CareError.Conflict, 409)]
    public async Task HandleCareException_PreservesExistingBehavior(
        CareError error,
        int expectedStatus)
    {
        await AssertProblemAsync(
            new CareException(error, "Care test message."),
            expectedStatus,
            "Care test message.");
    }

    private static async Task AssertProblemAsync(
        Exception exception,
        int expectedStatus,
        string expectedMessage)
    {
        using var services = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        await using var body = new MemoryStream();
        context.Response.Body = body;

        var handler = new ApiExceptionHandler(
            NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        var problem = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(expectedStatus));
            Assert.That(
                context.Response.ContentType,
                Does.StartWith("application/problem+json"));
            Assert.That(
                problem.GetProperty("status").GetInt32(),
                Is.EqualTo(expectedStatus));
            Assert.That(
                problem.GetProperty("detail").GetString(),
                Is.EqualTo(expectedMessage));
        });
    }
}