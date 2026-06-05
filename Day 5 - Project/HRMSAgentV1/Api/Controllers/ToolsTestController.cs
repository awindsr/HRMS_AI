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
            await Case("getEmployeeList (all)",                 () => _tools.GetEmployeeListAsync(null, null, null)),
            await Case("getEmployeeList (engineering, active)", () => _tools.GetEmployeeListAsync("engineering", "active", null)),
            await Case("getEmployeeDetails (E1001)",            () => _tools.GetEmployeeDetailsAsync("E1001")),
            await Case("getEmployeeDetails (E9999 -> 404)",     () => _tools.GetEmployeeDetailsAsync("E9999")),
            await Case("getEmployeeDetails (missing arg)",      () => _tools.GetEmployeeDetailsAsync("")),
            await Case("getTaskList (E1001)",                   () => _tools.GetTaskListAsync("E1001", null)),
            await Case("getTaskList (status=blocked)",          () => _tools.GetTaskListAsync(null, "blocked")),
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
