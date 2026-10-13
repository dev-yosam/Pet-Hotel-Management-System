using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/pets")]
public class PetsController(IPetService petService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PetResponse>>> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int? ownerId,
        CancellationToken cancellationToken)
    {
        var pets = await petService.GetAllAsync(
            ownerId,
            cancellationToken);

        return Ok(pets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PetResponse>> GetById(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var pet = await petService.GetByIdAsync(id, cancellationToken);

        if (pet is null)
        {
            return NotFound();
        }

        return Ok(pet);
    }

    [HttpPost]
    public async Task<ActionResult<PetResponse>> Create(
        [FromBody] PetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pet = await petService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = pet.Id },
                pet);
        }
        catch (ValidationException exception)
        {
            return CreateValidationError(exception);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PetResponse>> Update(
        [Range(1, int.MaxValue)] int id,
        [FromBody] PetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var pet = await petService.UpdateAsync(
                id,
                request,
                cancellationToken);

            if (pet is null)
            {
                return NotFound();
            }

            return Ok(pet);
        }
        catch (ValidationException exception)
        {
            return CreateValidationError(exception);
        }
    }

    private ActionResult CreateValidationError(
        ValidationException exception)
    {
        var members = exception.ValidationResult.MemberNames.ToList();

        if (members.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        else
        {
            foreach (var member in members)
            {
                ModelState.AddModelError(member, exception.Message);
            }
        }

        return ValidationProblem(ModelState);
    }
}