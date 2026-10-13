using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/owners")]
public class OwnersController(IOwnerService ownerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OwnerResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var owners = await ownerService.GetAllAsync(cancellationToken);

        return Ok(owners);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OwnerResponse>> GetById(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var owner = await ownerService.GetByIdAsync(id, cancellationToken);

        if (owner is null)
        {
            return NotFound();
        }

        return Ok(owner);
    }

    [HttpPost]
    public async Task<ActionResult<OwnerResponse>> Create(
        [FromBody] OwnerRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await ownerService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = owner.Id },
            owner);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OwnerResponse>> Update(
        [Range(1, int.MaxValue)] int id,
        [FromBody] OwnerRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await ownerService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (owner is null)
        {
            return NotFound();
        }

        return Ok(owner);
    }
}