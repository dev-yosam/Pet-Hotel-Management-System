using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;

namespace PetHotel.Api.Services;

public class CareService(PetHotelDbContext db) : ICareService
{
    // Include past stays only when they have care records to view.
    public async Task<List<CareStayDto>> GetStaysAsync(CancellationToken cancellationToken = default)
    {
        var stays = await db.Stays.AsNoTracking()
            .Where(stay => stay.Status == StayStatus.CheckedIn || stay.CareRecords.Any())
            .OrderBy(stay => stay.Status != StayStatus.CheckedIn)
            .ThenBy(stay => stay.Pet.Name)
            .ThenByDescending(stay => stay.ScheduledCheckIn)
            .Select(stay => new
            {
                stay.Id, PetName = stay.Pet.Name, OwnerName = stay.Pet.Owner.FullName,
                stay.Room.RoomNumber, stay.Status, stay.ActualCheckIn,
                stay.ScheduledCheckOut, stay.Pet.CareInstructions
            })
            .ToListAsync(cancellationToken);

        return stays.Select(stay => new CareStayDto(stay.Id, stay.PetName, stay.OwnerName,
            stay.RoomNumber, stay.Status.ToString(), stay.ActualCheckIn,
            stay.ScheduledCheckOut, stay.CareInstructions)).ToList();
    }

    public async Task<List<CareTaskDto>> GetTasksAsync(int stayId, CancellationToken cancellationToken = default)
    {
        if (!await db.Stays.AnyAsync(stay => stay.Id == stayId, cancellationToken))
            throw new CareException(CareError.NotFound, "Stay not found.");

        return await db.CareRecords.AsNoTracking()
            .Where(record => record.StayId == stayId)
            .OrderBy(record => record.ScheduledAt).ThenBy(record => record.Id)
            .Select(record => new CareTaskDto(record.Id, record.StayId, record.Description,
                record.ScheduledAt, record.IsCompleted, record.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<CareTaskDto> GetTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var record = await db.CareRecords.AsNoTracking()
            .SingleOrDefaultAsync(record => record.Id == taskId, cancellationToken)
            ?? throw new CareException(CareError.NotFound, "Care task not found.");
        return ToDto(record);
    }

    public async Task<CareTaskDto> CreateTaskAsync(int stayId, CreateCareTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var stay = await db.Stays.SingleOrDefaultAsync(stay => stay.Id == stayId, cancellationToken)
            ?? throw new CareException(CareError.NotFound, "Stay not found.");

        // Only checked-in stays can have new care tasks.
        RequireCheckedIn(stay);
        var description = request.Description?.Trim();
        if (string.IsNullOrEmpty(description) || description.Length > 500)
            throw new CareException(CareError.InvalidInput, "Description must contain 1 to 500 characters.");
        if (request.ScheduledAt is null)
            throw new CareException(CareError.InvalidInput, "Scheduled time is required.");
        if (stay.ActualCheckIn is null)
            throw new CareException(CareError.Conflict, "This stay has no actual check-in time.");

        var scheduledAt = request.ScheduledAt.Value.ToUniversalTime();
        // Care must be scheduled during the stay.
        if (scheduledAt < stay.ActualCheckIn.Value || scheduledAt >= stay.ScheduledCheckOut)
            throw new CareException(CareError.InvalidInput,
                "Scheduled time must be on or after check-in and before the planned check-out.");

        var record = new CareRecord { StayId = stayId, Description = description, ScheduledAt = scheduledAt };
        db.CareRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(record).ReloadAsync(cancellationToken);
        return ToDto(record);
    }

    public async Task<CareTaskDto> UpdateCompletionAsync(int taskId, bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var record = await FindTaskWithStayAsync(taskId, cancellationToken);
        RequireCheckedIn(record.Stay);

        // Keep the first completion time when the same request is sent again.
        if (record.IsCompleted != isCompleted)
        {
            record.IsCompleted = isCompleted;
            record.CompletedAt = isCompleted ? DateTimeOffset.UtcNow : null;
            await db.SaveChangesAsync(cancellationToken);
            // Return the timestamp stored by the database.
            await db.Entry(record).ReloadAsync(cancellationToken);
        }
        return ToDto(record);
    }

    public async Task DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var record = await FindTaskWithStayAsync(taskId, cancellationToken);
        RequireCheckedIn(record.Stay);

        // Only unfinished care tasks can be deleted.
        if (record.IsCompleted)
            throw new CareException(CareError.Conflict, "Completed care tasks cannot be deleted.");

        db.CareRecords.Remove(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<CareRecord> FindTaskWithStayAsync(int taskId, CancellationToken cancellationToken)
    {
        return await db.CareRecords.Include(record => record.Stay)
            .SingleOrDefaultAsync(record => record.Id == taskId, cancellationToken)
            ?? throw new CareException(CareError.NotFound, "Care task not found.");
    }

    private static void RequireCheckedIn(Stay stay)
    {
        // Past and cancelled stays are read-only.
        if (stay.Status != StayStatus.CheckedIn || stay.ActualCheckOut is not null)
            throw new CareException(CareError.Conflict, "Care tasks can only be changed for checked-in stays.");
    }

    private static CareTaskDto ToDto(CareRecord record) =>
        new(record.Id, record.StayId, record.Description, record.ScheduledAt, record.IsCompleted, record.CompletedAt);
}
