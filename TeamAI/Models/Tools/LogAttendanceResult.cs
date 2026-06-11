namespace TeamAI.Models.Tools;

/// <summary>Result of a LIVE logAttendance punch, for the agent to confirm back to the user.</summary>
public record LogAttendanceResult(
    bool Success,
    string EmployeeCode,
    string EmployeeName,
    string Type,        // check_in | check_out
    string Message);
