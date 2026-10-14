using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var booking = await bookingService.GetByIdAsync(
            id,
            cancellationToken);

        if (booking is null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(
        [FromBody] BookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = booking.Id },
            booking);
    }
}