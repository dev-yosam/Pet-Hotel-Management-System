using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PetHotel.Api.Services;

namespace PetHotel.Tests;

public class StayExceptionHandlerTests
{
    [TestCase(true, 400)]
    [TestCase(false, 500)]
    public async Task ValidationAndUnexpectedErrors_ReturnSafeResponses(bool validation, int expectedStatus)
    {
        using var services = new ServiceCollection().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        Exception error = validation
            ? new System.ComponentModel.DataAnnotations.ValidationException("Invalid input.")
            : new InvalidOperationException("Private database details.");
        await handler.TryHandleAsync(context, error, CancellationToken.None);
        body.Position = 0;
        using var reader = new StreamReader(body);
        var response = await reader.ReadToEndAsync();
        Assert.That(context.Response.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(response, Does.Not.Contain("Private database details"));
        if (validation) Assert.That(response, Does.Contain("Invalid input."));
    }
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
