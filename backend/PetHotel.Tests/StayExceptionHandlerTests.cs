using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

public class StayExceptionHandlerTests
{
    [TestCase(StayError.InvalidInput, 400)]
    [TestCase(StayError.NotFound, 404)]
    [TestCase(StayError.Conflict, 409)]
    public async Task HandleStayException_ReturnsExpectedProblem(
        StayError error,
        int expectedStatus)
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
            new StayException(error, "Stay test message."),
            CancellationToken.None);

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        var problem = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(
                context.Response.StatusCode,
                Is.EqualTo(expectedStatus));
            Assert.That(
                context.Response.ContentType,
                Does.StartWith("application/problem+json"));
            Assert.That(
                problem.GetProperty("status").GetInt32(),
                Is.EqualTo(expectedStatus));
            Assert.That(
                problem.GetProperty("detail").GetString(),
                Is.EqualTo("Stay test message."));
        });
    }
}
