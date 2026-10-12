using Microsoft.AspNetCore.Mvc;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get()
    {
        return Ok("Healthy");
    }
}
