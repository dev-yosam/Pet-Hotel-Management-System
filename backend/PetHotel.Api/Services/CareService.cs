using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Models;
using System.Data;

namespace PetHotel.Api.Services;

public class CareService(PetHotelDbContext db) : ICareService
{
    // List checked-in stays and other stays that have care records.
    public async Task<List<CareStayDto>> GetStaysAsync(CancellationToken cancellationToken = default)
    {
        var stays = await db.Stays.AsNoTracking()
            .Where(stay => stay.Status == StayStatus.CheckedIn || stay.CareRecords.Any())
            .OrderBy(stay => stay.Status != StayStatus.CheckedIn)
            .ThenBy(stay => stay.Pet.Name)
            .ThenByDescending(stay => stay.ScheduledCheckIn)
            .Select(stay => new
            {
                stay.Id,
                PetName = stay.Pet.Name,
                OwnerName = stay.Pet.Owner.FullName,
                stay.Room.RoomNumber,
                stay.Status,
                stay.ActualCheckIn,
                stay.ScheduledCheckOut,
                stay.Pet.CareInstructions
            })
            .ToListAsync(cancellationToken);

        return stays.Select(stay => new CareStayDto(stay.Id, stay.PetName, stay.OwnerName,
            stay.RoomNumber, stay.Status.ToString(), stay.ActualCheckIn,
            stay.ScheduledCheckOut, stay.CareInstructions)).ToList();
    }

    // Get the care tasks for one stay, ordered by scheduled time.
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

    // Get one care task, or report that it was not found.
    public async Task<CareTaskDto> GetTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var record = await db.CareRecords.AsNoTracking()
            .SingleOrDefaultAsync(record => record.Id == taskId, cancellationToken)
            ?? throw new CareException(CareError.NotFound, "Care task not found.");
        return ToDto(record);
    }

    // Add a task only for a checked-in pet, after checking the description and time.
    public async Task<CareTaskDto> CreateTaskAsync(
    int stayId,
    CreateCareTaskRequest request,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var stay = await LockStayAsync(stayId, cancellationToken);
        RequireCheckedIn(stay);

        var description = request.Description?.Trim();

        if (string.IsNullOrEmpty(description) || description.Length > 500)
            throw new CareException(
                CareError.InvalidInput,
                "Description must contain 1 to 500 characters.");

        if (request.ScheduledAt is null)
            throw new CareException(
                CareError.InvalidInput,
                "Scheduled time is required.");

        if (stay.ActualCheckIn is null)
            throw new CareException(
                CareError.Conflict,
                "This stay has no actual check-in time.");

        var scheduledAt = request.ScheduledAt.Value.ToUniversalTime();

        if (scheduledAt < stay.ActualCheckIn.Value ||
            scheduledAt >= stay.ScheduledCheckOut)
            throw new CareException(
                CareError.InvalidInput,
                "Scheduled time must be on or after check-in and before the planned check-out.");

        var record = new CareRecord
        {
            StayId = stayId,
            Description = description,
            ScheduledAt = scheduledAt
        };

        db.CareRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(record).ReloadAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ToDto(record);
    }

    // Mark a task as completed or unfinished while the pet is checked in
    public async Task<CareTaskDto> UpdateCompletionAsync(
    int taskId,
    bool isCompleted,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var record = await FindTaskWithStayAsync(taskId, cancellationToken);
        RequireCheckedIn(record.Stay);

        // Repeated requests preserve the first completion timestamp.
        if (record.IsCompleted != isCompleted)
        {
            record.IsCompleted = isCompleted;
            record.CompletedAt = isCompleted ? DateTimeOffset.UtcNow : null;

            await db.SaveChangesAsync(cancellationToken);
            await db.Entry(record).ReloadAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return ToDto(record);
    }

    // Delete only unfinished tasks for a checked-in pet.
    public async Task DeleteTaskAsync(
    int taskId,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var record = await FindTaskWithStayAsync(taskId, cancellationToken);
        RequireCheckedIn(record.Stay);

        if (record.IsCompleted)
            throw new CareException(
                CareError.Conflict,
                "Completed care tasks cannot be deleted.");

        db.CareRecords.Remove(record);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    // Lock the stay so check-out cannot happen while a care task is being changed.
    private async Task<Stay> LockStayAsync(
     int stayId,
     CancellationToken cancellationToken)
    {
        var stays = await db.Stays
            .FromSqlInterpolated(
                $"""SELECT * FROM "Stays" WHERE "Id" = {stayId} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        var stay = stays.SingleOrDefault()
            ?? throw new CareException(
                CareError.NotFound,
                "Stay not found.");

        // Refresh entities that this context may already be tracking.
        await db.Entry(stay).ReloadAsync(cancellationToken);

        return stay;
    }

    // Lock the related stay and load the latest task before making changes
    private async Task<CareRecord> FindTaskWithStayAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        var stayId = await db.CareRecords
            .AsNoTracking()
            .Where(record => record.Id == taskId)
            .Select(record => (int?)record.StayId)
            .SingleOrDefaultAsync(cancellationToken);

        if (stayId is null)
            throw new CareException(
                CareError.NotFound,
                "Care task not found.");

        // Lock the stay before reading the task's current state.
        var stay = await LockStayAsync(stayId.Value, cancellationToken);

        var record = await db.CareRecords
            .SingleOrDefaultAsync(
                record => record.Id == taskId,
                cancellationToken)
            ?? throw new CareException(
                CareError.NotFound,
                "Care task not found.");

        await db.Entry(record).ReloadAsync(cancellationToken);

        if (db.Entry(record).State == EntityState.Detached)
            throw new CareException(
                CareError.NotFound,
                "Care task not found.");

        if (record.StayId != stay.Id)
            throw new CareException(
                CareError.Conflict,
                "The care task has changed. Refresh and try again.");

        record.Stay = stay;

        return record;
    }

    // stop care changes if the pet is not currently checked in.
    private static void RequireCheckedIn(Stay stay)
    {
        if (stay.Status != StayStatus.CheckedIn || stay.ActualCheckOut is not null)
            throw new CareException(CareError.Conflict, "Care tasks can only be changed for checked-in stays.");
    }

    // Copy the task details into the response sent to the frontend.
    private static CareTaskDto ToDto(CareRecord record) =>
        new(record.Id, record.StayId, record.Description, record.ScheduledAt, record.IsCompleted, record.CompletedAt);
}
