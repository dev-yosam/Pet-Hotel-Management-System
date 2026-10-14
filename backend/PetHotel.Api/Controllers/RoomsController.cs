using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(IRoomService roomService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var rooms = await roomService.GetAllAsync(cancellationToken);
        return Ok(rooms);
    }

    [HttpGet("available")]
    public async Task<ActionResult<List<RoomResponse>>> GetAvailable(
        [FromQuery, Required] DateTimeOffset? checkIn,
        [FromQuery, Required] DateTimeOffset? checkOut,
        CancellationToken cancellationToken)
    {
        if (checkOut!.Value <= checkIn!.Value)
        {
            ModelState.AddModelError(
                nameof(checkOut),
                "Check-out must be after check-in.");

            return ValidationProblem(
                statusCode: StatusCodes.Status400BadRequest,
                modelStateDictionary: ModelState);
        }

        var rooms = await roomService.GetAvailableAsync(
            checkIn.Value,
            checkOut.Value,
            cancellationToken);

        return Ok(rooms);
    }
}