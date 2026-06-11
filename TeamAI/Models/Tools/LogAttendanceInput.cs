namespace TeamAI.Models.Tools;

/// <summary>
/// Clean agent-facing input for a LIVE attendance punch. The member is identified by
/// EmployeeCode (the same id the read tool exposes); the backend resolves it and records the
/// punch at the current time — HRMS timestamps it on submission, so there is no date/time
/// input. Type is "check_in" or "check_out".
/// </summary>
public record LogAttendanceInput(
    string EmployeeCode,
    string Type,          // check_in | check_out
    int? TeamId = null,
    string? Location = null,
    string? Comment = null);
