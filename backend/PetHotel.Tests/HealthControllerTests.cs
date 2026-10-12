using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.Controllers;

namespace PetHotel.Tests;

public class HealthControllerTests
{
    [Test]
    public void Get_ReturnsHealthyResponse()
    {
        var controller = new HealthController();

        var response = controller.Get();

        Assert.That(response.Result, Is.TypeOf<OkObjectResult>());
        var result = (OkObjectResult)response.Result!;
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Value, Is.EqualTo("Healthy"));
    }
}
