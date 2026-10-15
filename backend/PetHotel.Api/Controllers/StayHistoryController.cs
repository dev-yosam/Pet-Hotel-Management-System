using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/stay-history")]
public class StayHistoryController(IStayHistoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StayHistoryResult>> Search(
        [FromQuery] StayHistoryQuery query, CancellationToken cancellationToken) =>
        Ok(await service.SearchAsync(query, cancellationToken));
}
