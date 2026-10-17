using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PetHotel.Api.DTOs;
using PetHotel.Api.Interfaces;

namespace PetHotel.Api.Controllers;

[ApiController]
[Route("api/care")]
public class CareController(ICareService careService) : ControllerBase
{
    [HttpGet("stays")]
    public async Task<ActionResult<List<CareStayDto>>> GetStays(CancellationToken cancellationToken) =>
        Ok(await careService.GetStaysAsync(cancellationToken));

    [HttpGet("stays/{stayId:int}/tasks")]
    public async Task<ActionResult<List<CareTaskDto>>> GetTasks([Range(1, int.MaxValue)] int stayId, CancellationToken cancellationToken) =>
        Ok(await careService.GetTasksAsync(stayId, cancellationToken));

    [HttpGet("tasks/{taskId:int}")]
    public async Task<ActionResult<CareTaskDto>> GetTask([Range(1, int.MaxValue)] int taskId, CancellationToken cancellationToken) =>
        Ok(await careService.GetTaskAsync(taskId, cancellationToken));

    [HttpPost("stays/{stayId:int}/tasks")]
    public async Task<ActionResult<CareTaskDto>> CreateTask([Range(1, int.MaxValue)] int stayId, CreateCareTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await careService.CreateTaskAsync(stayId, request, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { taskId = task.Id }, task);
    }

    [HttpPatch("tasks/{taskId:int}/completion")]
    public async Task<ActionResult<CareTaskDto>> UpdateCompletion([Range(1, int.MaxValue)] int taskId, UpdateCareCompletionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await careService.UpdateCompletionAsync(taskId, request.IsCompleted!.Value, cancellationToken));

    [HttpDelete("tasks/{taskId:int}")]
    public async Task<IActionResult> DeleteTask([Range(1, int.MaxValue)] int taskId, CancellationToken cancellationToken)
    {
        await careService.DeleteTaskAsync(taskId, cancellationToken);
        return NoContent();
    }
}
