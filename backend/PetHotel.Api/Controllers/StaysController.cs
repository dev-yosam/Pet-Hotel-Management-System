using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/stays")]
public class StaysController(IStayService stayService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StayResponse>> GetById(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var stay = await stayService.GetByIdAsync(
            id,
            cancellationToken);

        if (stay is null)
        {
            return NotFound();
        }

        return Ok(stay);
    }

    [HttpPost("{id:int}/check-in")]
    public async Task<ActionResult<StayResponse>> CheckIn(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var stay = await stayService.CheckInAsync(
            id,
            cancellationToken);

        return Ok(stay);
    }
}