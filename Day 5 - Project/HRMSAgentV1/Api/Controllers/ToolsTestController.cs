using HrmsAgent.Tools;
using Microsoft.AspNetCore.Mvc;

namespace HRMSAgentV1.Api.Controllers;

/// <summary>
/// Web equivalent of the guide's <c>--test</c> mode. Exercises each read tool (including the
/// 404 and missing-argument error paths) and returns the raw tool outputs. Every call also
/// lands in <c>logs/api-calls.log</c> via the ApiLogger — the Day 5 "API call log" deliverable.
/// No Azure OpenAI key required.
/// </summary>
[ApiController]
[Route("api/v1/_test")]
public sealed class ToolsTestController : ControllerBase
{
    private readonly HrmsTools _tools;

    public ToolsTestController(HrmsTools tools) => _tools = tools;

    [HttpGet]
    public async Task<IActionResult> RunAll()
    {
        var results = new List<object>
        {
            // ---- Day 5 read tools ----
            await Case("getEmployeeList (all)",                 () => _tools.GetEmployeeListAsync(null, null, null)),
            await Case("getEmployeeList (engineering, active)", () => _tools.GetEmployeeListAsync("engineering", "active", null)),
            await Case("getEmployeeDetails (E1001)",            () => _tools.GetEmployeeDetailsAsync("E1001")),
            await Case("getEmployeeDetails (E9999 -> 404)",     () => _tools.GetEmployeeDetailsAsync("E9999")),
            await Case("getEmployeeDetails (missing arg)",      () => _tools.GetEmployeeDetailsAsync("")),
            await Case("getTaskList (E1001)",                   () => _tools.GetTaskListAsync("E1001", null)),
            await Case("getTaskList (status=blocked)",          () => _tools.GetTaskListAsync(null, "blocked")),

            // ---- Day 6 write tools: confirmation gate FIRST ----
            await Case("createTask (unconfirmed -> blocked)",   () => _tools.CreateTaskAsync("Draft release notes", null, "E1001", "high", "2026-06-30", confirmed: false)),
            await Case("createTask (confirmed -> created)",     () => _tools.CreateTaskAsync("Draft release notes", null, "E1001", "high", "2026-06-30", confirmed: true)),
            await Case("createTask (missing title)",            () => _tools.CreateTaskAsync("", null, null, null, null, confirmed: true)),
            await Case("createTask (bad priority)",             () => _tools.CreateTaskAsync("X", null, null, "urgent", null, confirmed: true)),

            await Case("assignTask (unconfirmed -> blocked)",   () => _tools.AssignTaskAsync("T-504", "E1002", confirmed: false)),
            await Case("assignTask (confirmed -> reassigned)",  () => _tools.AssignTaskAsync("T-504", "E1002", confirmed: true)),
            await Case("assignTask (unknown task -> 404)",      () => _tools.AssignTaskAsync("T-999", "E1002", confirmed: true)),

            await Case("markAttendance (unconfirmed -> blocked)", () => _tools.MarkAttendanceAsync("E1002", null, "present", "09:15", "18:30", null, confirmed: false)),
            await Case("markAttendance (confirmed -> recorded)",  () => _tools.MarkAttendanceAsync("E1002", null, "present", "09:15", "18:30", null, confirmed: true)),
            await Case("markAttendance (overwrite seed 5 Jun)",   () => _tools.MarkAttendanceAsync("E1001", "2026-06-05", "present", null, null, "client site", confirmed: true)),
            await Case("markAttendance (future date -> rejected)",() => _tools.MarkAttendanceAsync("E1002", "2099-01-01", "present", null, null, null, confirmed: true)),
            await Case("markAttendance (bad status)",             () => _tools.MarkAttendanceAsync("E1002", null, "vacation", null, null, null, confirmed: true)),

            // ---- deleteTask: double-confirm via re-typed token ----
            await Case("deleteTask (bare yes -> blocked)",      () => _tools.DeleteTaskAsync("T-505", confirmationToken: "yes")),
            await Case("deleteTask (token matches -> deleted)",  () => _tools.DeleteTaskAsync("T-505", confirmationToken: "T-505")),
            await Case("deleteTask (already gone -> 404)",      () => _tools.DeleteTaskAsync("T-505", confirmationToken: "T-505")),
        };

        return Content(
            System.Text.Json.JsonSerializer.Serialize(new { cases = results },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            "application/json");
    }

    private static async Task<object> Case(string label, Func<Task<string>> call)
    {
        var raw = await call();
        // Re-parse the tool's JSON string so it nests cleanly in the response instead of being escaped.
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        return new { label, result = doc.RootElement.Clone() };
    }
}
