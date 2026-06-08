using System.Text.Json;
using System.Text.Json.Serialization;
using HrmsAgent.Models;

namespace HrmsAgent.Tools;

/// <summary>
/// The tools the LLM can call. Each builds the request, calls the wrapper, and returns a
/// JSON string (success payload or a clean { "error": ... } object) — exactly what the
/// function-calling loop feeds back to the model.
///
/// Day 5: three read-only tools (getEmployeeList, getEmployeeDetails, getTaskList).
/// Day 6: four write tools (createTask, assignTask, markAttendance, deleteTask). Every write
/// passes through the CONFIRMATION GATE before any HTTP call — if the model has not set
/// confirmed=true (and, for deletes, re-typed the task ID), the tool returns
/// "confirmation_required" and makes no change. See docs Day 6 / confirmation-flow.md.
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

    /// <summary>getTaskDetails → GET /api/v1/tasks/{id}. Lets the agent look up one task before a write.</summary>
    public async Task<string> GetTaskDetailsAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return MissingArg("taskId");

        var res = await _api.GetAsync<EmployeeTask>($"/api/v1/tasks/{Uri.EscapeDataString(taskId)}");
        return res.Ok ? Json(res.Data) : ErrorJson(res);
    }

    // ========================================================================
    // Day 6 — WRITE tools. Each enforces the confirmation gate FIRST, then
    // validates arguments, and only then performs the HTTP write. A blocked
    // confirmation never reaches the network — it is the binding-layer guard
    // described in confirmation-flow.md §3.
    //
    // NOTE: `confirmed` is supplied by the model here (it sets it true only after
    // summarizing and getting an explicit user "yes", per system-prompt §7). A
    // production binding layer would set this flag server-side from a real UI
    // confirmation rather than trusting the model. Role/RBAC checks (who may write,
    // self vs HR) are likewise prompt-layer only in this build — see the Day 6 docs.
    // ========================================================================

    private static readonly string[] TaskStatuses = { "open", "in_progress", "done", "blocked" };
    private static readonly string[] Priorities = { "low", "medium", "high" };
    private static readonly string[] AttendanceStatuses = { "present", "absent", "wfh", "leave", "half_day" };

    /// <summary>createTask → POST /api/v1/tasks (R2 soft write)</summary>
    public async Task<string> CreateTaskAsync(
        string title, string? description, string? assigneeId, string? priority, string? dueDate, bool confirmed)
    {
        if (!confirmed) return ConfirmationRequired("createTask");

        if (string.IsNullOrWhiteSpace(title))
            return MissingArg("title");
        if (priority is not null && !Priorities.Contains(priority, StringComparer.OrdinalIgnoreCase))
            return InvalidArg($"priority must be one of: {string.Join(", ", Priorities)}.");
        if (!TryParseFutureSafeDate(dueDate, out var due, allowPast: false, out var dueErr))
            return InvalidArg(dueErr!);

        var body = new
        {
            title,
            description,
            assigneeId,
            priority = priority ?? "medium",
            dueDate = due?.ToString("yyyy-MM-dd")
        };
        var res = await _api.PostAsync<CreateTaskResult>("/api/v1/tasks", body);
        return res.Ok ? Json(res.Data) : ErrorJson(res);
    }

    /// <summary>assignTask → PATCH /api/v1/tasks/{taskId}/assignment (R3 hard write)</summary>
    public async Task<string> AssignTaskAsync(string taskId, string assigneeId, bool confirmed)
    {
        if (!confirmed) return ConfirmationRequired("assignTask");

        if (string.IsNullOrWhiteSpace(taskId)) return MissingArg("taskId");
        if (string.IsNullOrWhiteSpace(assigneeId)) return MissingArg("assigneeId");

        var body = new { assigneeId };
        var res = await _api.PatchAsync<AssignTaskResult>(
            $"/api/v1/tasks/{Uri.EscapeDataString(taskId)}/assignment", body);
        return res.Ok ? Json(res.Data) : ErrorJson(res);
    }

    /// <summary>markAttendance → POST /api/v1/attendance (R2 self / R3 HR)</summary>
    public async Task<string> MarkAttendanceAsync(
        string employeeId, string? date, string status, string? checkIn, string? checkOut, string? note, bool confirmed)
    {
        if (!confirmed) return ConfirmationRequired("markAttendance");

        if (string.IsNullOrWhiteSpace(employeeId)) return MissingArg("employeeId");
        if (string.IsNullOrWhiteSpace(status)) return MissingArg("status");
        if (!AttendanceStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            return InvalidArg($"status must be one of: {string.Join(", ", AttendanceStatuses)}.");
        // AT-6: you cannot be present on a day that has not happened.
        if (!TryParseFutureSafeDate(date, out var when, allowPast: true, out var dateErr))
            return InvalidArg(dateErr!);
        // AT-2: check-out must be after check-in.
        if (checkIn is not null && checkOut is not null &&
            string.CompareOrdinal(checkOut, checkIn) <= 0)
            return InvalidArg("checkOut must be later than checkIn.");

        var body = new
        {
            employeeId,
            date = (when ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd"),
            status,
            checkIn,
            checkOut,
            note
        };
        var res = await _api.PostAsync<MarkAttendanceResult>("/api/v1/attendance", body);
        return res.Ok ? Json(res.Data) : ErrorJson(res);
    }

    /// <summary>deleteTask → DELETE /api/v1/tasks/{taskId} (R4 destructive, double-confirm)</summary>
    public async Task<string> DeleteTaskAsync(string taskId, string? confirmationToken)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return MissingArg("taskId");

        // CF-8 / DR-4: a bare "yes" is not enough. The user must re-type the exact task ID,
        // which the model passes as confirmationToken. No token match → no delete.
        if (!string.Equals(confirmationToken, taskId, StringComparison.Ordinal))
            return Json(new
            {
                error = "confirmation_required",
                message = $"Deleting {taskId} is permanent and cannot be undone. To confirm, re-type the task ID '{taskId}'."
            });

        var res = await _api.DeleteAsync<DeleteTaskResult>($"/api/v1/tasks/{Uri.EscapeDataString(taskId)}");
        return res.Ok ? Json(res.Data) : ErrorJson(res);
    }

    // ---- write-tool result envelopes (mirror the mock API output shapes) ----
    private sealed record CreateTaskResult(
        [property: JsonPropertyName("taskId")] string TaskId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("assigneeId")] string? AssigneeId,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("dueDate")] string DueDate,
        [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
        [property: JsonPropertyName("notificationSent")] bool NotificationSent);

    private sealed record AssignTaskResult(
        [property: JsonPropertyName("taskId")] string TaskId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("previousAssigneeId")] string? PreviousAssigneeId,
        [property: JsonPropertyName("newAssigneeId")] string NewAssigneeId,
        [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt,
        [property: JsonPropertyName("notificationSent")] bool NotificationSent);

    private sealed record MarkAttendanceResult(
        [property: JsonPropertyName("attendanceId")] string AttendanceId,
        [property: JsonPropertyName("employeeId")] string EmployeeId,
        [property: JsonPropertyName("date")] string Date,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("recordedBy")] string RecordedBy,
        [property: JsonPropertyName("recordedAt")] DateTimeOffset RecordedAt,
        [property: JsonPropertyName("wasOverwrite")] bool WasOverwrite);

    private sealed record DeleteTaskResult(
        [property: JsonPropertyName("taskId")] string TaskId,
        [property: JsonPropertyName("deleted")] bool Deleted,
        [property: JsonPropertyName("deletedAt")] DateTimeOffset DeletedAt,
        [property: JsonPropertyName("deletedBy")] string DeletedBy);

    // ---- helpers ----
    private static string ConfirmationRequired(string tool) => Json(new
    {
        error = "confirmation_required",
        message = $"{tool} changes data. Summarize the action for the user and obtain an explicit " +
                  "confirmation, then call again with confirmed=true."
    });

    private static string MissingArg(string name) =>
        Json(new { error = "missing_argument", message = $"{name} is required." });

    private static string InvalidArg(string message) =>
        Json(new { error = "invalid_argument", message });

    /// <summary>
    /// Parse an optional YYYY-MM-DD date. Null/blank is allowed (returns null). A past date is
    /// rejected unless allowPast is true; a future date is always allowed only when the caller
    /// permits it (createTask due dates yes; attendance no — AT-6).
    /// </summary>
    private static bool TryParseFutureSafeDate(string? raw, out DateOnly? value, bool allowPast, out string? error)
    {
        value = null;
        error = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;

        if (!DateOnly.TryParse(raw, out var d))
        {
            error = "date must be in YYYY-MM-DD format.";
            return false;
        }
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!allowPast && d < today)
        {
            error = "date cannot be in the past.";
            return false;
        }
        if (allowPast && d > today)
        {
            // attendance: future-dated entries are nonsensical (AT-6)
            error = "attendance cannot be recorded for a future date.";
            return false;
        }
        value = d;
        return true;
    }
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
