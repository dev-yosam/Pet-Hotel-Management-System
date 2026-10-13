using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class PetService(PetHotelDbContext dbContext) : IPetService
{
    public async Task<List<PetResponse>> GetAllAsync(
        int? ownerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Pets.AsNoTracking();

        if (ownerId.HasValue)
        {
            query = query.Where(pet => pet.OwnerId == ownerId.Value);
        }

        return await query
            .OrderBy(pet => pet.Name)
            .ThenBy(pet => pet.Id)
            .Select(pet => new PetResponse
            {
                Id = pet.Id,
                OwnerId = pet.OwnerId,
                Name = pet.Name,
                Species = pet.Species,
                Breed = pet.Breed,
                CareInstructions = pet.CareInstructions
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PetResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var pet = await dbContext.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                pet => pet.Id == id,
                cancellationToken);

        return pet is null ? null : ToResponse(pet);
    }

    public async Task<PetResponse> CreateAsync(
        PetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await ValidateOwnerAsync(request.OwnerId, cancellationToken);

        var pet = new Pet
        {
            OwnerId = request.OwnerId,
            Name = request.Name.Trim(),
            Species = request.Species.Trim(),
            Breed = NormalizeOptionalText(request.Breed),
            CareInstructions = NormalizeOptionalText(request.CareInstructions)
        };

        dbContext.Pets.Add(pet);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(pet);
    }

    public async Task<PetResponse?> UpdateAsync(
        int id,
        PetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var pet = await dbContext.Pets
            .FirstOrDefaultAsync(
                pet => pet.Id == id,
                cancellationToken);

        if (pet is null)
        {
            return null;
        }

        await ValidateOwnerAsync(request.OwnerId, cancellationToken);

        pet.OwnerId = request.OwnerId;
        pet.Name = request.Name.Trim();
        pet.Species = request.Species.Trim();
        pet.Breed = NormalizeOptionalText(request.Breed);
        pet.CareInstructions = NormalizeOptionalText(request.CareInstructions);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(pet);
    }

    private async Task ValidateOwnerAsync(
        int ownerId,
        CancellationToken cancellationToken)
    {
        var ownerExists = await dbContext.Owners
            .AnyAsync(owner => owner.Id == ownerId, cancellationToken);

        if (!ownerExists)
        {
            throw new ValidationException(
                new ValidationResult(
                    "The selected owner does not exist.",
                    new[] { nameof(PetRequest.OwnerId) }),
                null,
                ownerId);
        }
    }

    private static void ValidateRequest(PetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Validator.ValidateObject(
            request,
            new ValidationContext(request),
            validateAllProperties: true);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static PetResponse ToResponse(Pet pet)
    {
        return new PetResponse
        {
            Id = pet.Id,
            OwnerId = pet.OwnerId,
            Name = pet.Name,
            Species = pet.Species,
            Breed = pet.Breed,
            CareInstructions = pet.CareInstructions
        };
    }
}