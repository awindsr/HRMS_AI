using System.Text.Json;
using System.Text.Json.Serialization;
using HrmsAgent.Models;

namespace HrmsAgent.Tools;

/// <summary>
/// The three read-only tools the LLM can call. Each builds a query string, calls the
/// wrapper, and returns a JSON string (success payload or a clean { "error": ... } object)
/// — exactly what the function-calling loop feeds back to the model.
/// </summary>
public sealed class HrmsTools
{
    private readonly HrmsApiClient _api;
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public HrmsTools(HrmsApiClient api) => _api = api;

    // ---- response envelopes from the mock API ----
    private sealed record EmployeeListResponse(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("employees")] List<Employee> Employees);

    private sealed record TaskListResponse(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("tasks")] List<EmployeeTask> Tasks);

    /// <summary>getEmployeeList → GET /api/v1/employees</summary>
    public async Task<string> GetEmployeeListAsync(string? department, string? status, int? limit)
    {
        var qs = BuildQuery(("department", department), ("status", status),
                            ("limit", limit?.ToString()));
        var res = await _api.GetAsync<EmployeeListResponse>($"/api/v1/employees{qs}");
        return res.Ok
            ? Json(new { total = res.Data!.Total, employees = res.Data.Employees })
            : ErrorJson(res);
    }

    /// <summary>getEmployeeDetails → GET /api/v1/employees/{id}</summary>
    public async Task<string> GetEmployeeDetailsAsync(string employeeId)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            return Json(new { error = "missing_argument", message = "employeeId is required." });

        var res = await _api.GetAsync<Employee>($"/api/v1/employees/{Uri.EscapeDataString(employeeId)}");
        return res.Ok ? Json(res.Data) : ErrorJson(res);
    }

    /// <summary>getTaskList → GET /api/v1/tasks</summary>
    public async Task<string> GetTaskListAsync(string? employeeId, string? status)
    {
        var qs = BuildQuery(("employeeId", employeeId), ("status", status));
        var res = await _api.GetAsync<TaskListResponse>($"/api/v1/tasks{qs}");
        return res.Ok
            ? Json(new { total = res.Data!.Total, tasks = res.Data.Tasks })
            : ErrorJson(res);
    }

    // ---- helpers ----
    private static string BuildQuery(params (string Key, string? Value)[] parts)
    {
        var pairs = parts.Where(p => !string.IsNullOrWhiteSpace(p.Value))
                         .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var joined = string.Join("&", pairs);
        return joined.Length == 0 ? "" : "?" + joined;
    }

    private static string Json(object? o) => JsonSerializer.Serialize(o, JsonOpts);

    private static string ErrorJson<T>(ApiResult<T> res) =>
        Json(new { error = res.ErrorCode, message = res.ErrorMessage });
}
