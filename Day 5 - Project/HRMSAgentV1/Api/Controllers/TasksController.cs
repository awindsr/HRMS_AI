using HrmsAgent.Data;
using HrmsAgent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSAgentV1.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly HrmsDbContext _db;
    public TasksController(HrmsDbContext db) => _db = db;

    // GET /api/v1/tasks?employeeId=&status=
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? employeeId, [FromQuery] string? status)
    {
        IQueryable<EmployeeTask> q = _db.Tasks;
        if (!string.IsNullOrWhiteSpace(employeeId))
            q = q.Where(t => t.EmployeeId.ToLower() == employeeId.ToLower());
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(t => t.Status.ToLower() == status.ToLower());

        var list = await q.OrderBy(t => t.TaskId).ToListAsync();
        return Ok(new { total = list.Count, tasks = list });
    }

    // GET /api/v1/tasks/{taskId}  (getTaskDetails — lets the agent look up one task before a write)
    [HttpGet("{taskId}")]
    public async Task<IActionResult> GetById(string taskId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId.ToLower() == taskId.ToLower());
        return task is null
            ? NotFound(new { error = "task_not_found", taskId })
            : Ok(task);
    }

    // ---- Day 6 write endpoints ----

    public sealed record CreateTaskRequest(
        string? Title, string? Description, string? AssigneeId, string? Priority, DateOnly? DueDate);

    // POST /api/v1/tasks  (createTask, R2)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Title))
            return BadRequest(new { error = "invalid_argument", message = "title is required." });

        var assigneeKnown = req.AssigneeId is not null &&
            await _db.Employees.AnyAsync(e => e.EmployeeId.ToLower() == req.AssigneeId.ToLower());

        var task = new EmployeeTask
        {
            TaskId = await NextTaskIdAsync(),
            EmployeeId = assigneeKnown ? req.AssigneeId! : "",
            Title = req.Title!.Trim(),
            Description = req.Description ?? "",
            Status = "open",
            Priority = string.IsNullOrWhiteSpace(req.Priority) ? "medium" : req.Priority!.ToLowerInvariant(),
            DueDate = req.DueDate ?? DateOnly.FromDateTime(DateTime.Today)
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return Created($"/api/v1/tasks/{task.TaskId}", new
        {
            taskId = task.TaskId,
            title = task.Title,
            assigneeId = assigneeKnown ? req.AssigneeId : null,
            status = task.Status,
            priority = task.Priority,
            dueDate = task.DueDate.ToString("yyyy-MM-dd"),
            createdAt = DateTimeOffset.Now,
            notificationSent = assigneeKnown
        });
    }

    public sealed record AssignTaskRequest(string? AssigneeId);

    // PATCH /api/v1/tasks/{taskId}/assignment  (assignTask, R3)
    [HttpPatch("{taskId}/assignment")]
    public async Task<IActionResult> Assign(string taskId, [FromBody] AssignTaskRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.AssigneeId))
            return BadRequest(new { error = "invalid_argument", message = "assigneeId is required." });

        var existing = await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId.ToLower() == taskId.ToLower());
        if (existing is null)
            return NotFound(new { error = "task_not_found", taskId });

        if (!await _db.Employees.AnyAsync(e => e.EmployeeId.ToLower() == req.AssigneeId.ToLower()))
            return BadRequest(new { error = "invalid_argument", message = $"No employee with ID {req.AssigneeId}." });

        var previous = string.IsNullOrEmpty(existing.EmployeeId) ? null : existing.EmployeeId;

        // The models are immutable records; update the column directly with ExecuteUpdate.
        await _db.Tasks.Where(t => t.TaskId == existing.TaskId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.EmployeeId, req.AssigneeId!));

        return Ok(new
        {
            taskId = existing.TaskId,
            title = existing.Title,
            previousAssigneeId = previous,
            newAssigneeId = req.AssigneeId,
            updatedAt = DateTimeOffset.Now,
            notificationSent = true
        });
    }

    // DELETE /api/v1/tasks/{taskId}  (deleteTask, R4 — hard delete in the mock)
    [HttpDelete("{taskId}")]
    public async Task<IActionResult> Delete(string taskId)
    {
        var existing = await _db.Tasks.FirstOrDefaultAsync(t => t.TaskId.ToLower() == taskId.ToLower());
        if (existing is null)
            return NotFound(new { error = "task_not_found", taskId });

        await _db.Tasks.Where(t => t.TaskId == existing.TaskId).ExecuteDeleteAsync();

        return Ok(new
        {
            taskId = existing.TaskId,
            deleted = true,
            deletedAt = DateTimeOffset.Now,
            deletedBy = "system" // no auth in the mock; a real system records the HR/Admin user
        });
    }

    /// <summary>Next sequential task ID, e.g. "T-526", from the highest existing numeric suffix.</summary>
    private async Task<string> NextTaskIdAsync()
    {
        var ids = await _db.Tasks.Select(t => t.TaskId).ToListAsync();
        var max = ids
            .Select(s => int.TryParse(s.Replace("T-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(500)
            .Max();
        return $"T-{max + 1}";
    }
}
