namespace TeamAI.Models.Tools;

/// <summary>Result of a LIVE logAttendance punch for the signed-in user, for the agent to confirm back.</summary>
public record LogAttendanceResult(
    bool Success,
    string Type,        // check_in | check_out
    string Message);
