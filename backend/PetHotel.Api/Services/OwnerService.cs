using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class OwnerService(PetHotelDbContext dbContext) : IOwnerService
{
    public async Task<List<OwnerResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Owners
            .AsNoTracking()
            .OrderBy(owner => owner.FullName)
            .ThenBy(owner => owner.Id)
            .Select(owner => new OwnerResponse
            {
                Id = owner.Id,
                FullName = owner.FullName,
                Phone = owner.Phone,
                Email = owner.Email
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var owner = await dbContext.Owners
            .AsNoTracking()
            .FirstOrDefaultAsync(
                owner => owner.Id == id,
                cancellationToken);

        return owner is null ? null : ToResponse(owner);
    }

    public async Task<OwnerResponse> CreateAsync(
        OwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var owner = new Owner
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = NormalizeEmail(request.Email)
        };

        dbContext.Owners.Add(owner);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(owner);
    }

    public async Task<OwnerResponse?> UpdateAsync(
        int id,
        OwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var owner = await dbContext.Owners
            .FirstOrDefaultAsync(
                owner => owner.Id == id,
                cancellationToken);

        if (owner is null)
        {
            return null;
        }

        owner.FullName = request.FullName.Trim();
        owner.Phone = request.Phone.Trim();
        owner.Email = NormalizeEmail(request.Email);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(owner);
    }

    private static void ValidateRequest(OwnerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Validator.ValidateObject(
            request,
            new ValidationContext(request),
            validateAllProperties: true);
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    private static OwnerResponse ToResponse(Owner owner)
    {
        return new OwnerResponse
        {
            Id = owner.Id,
            FullName = owner.FullName,
            Phone = owner.Phone,
            Email = owner.Email
        };
    }
}