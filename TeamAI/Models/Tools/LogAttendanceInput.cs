namespace TeamAI.Models.Tools;

/// <summary>
/// Clean agent-facing input for a LIVE attendance punch for the SIGNED-IN user. There is no
/// employee identifier — the punch is always for the signed-in user, resolved from their token on
/// the backend. The punch is recorded at the current time (HRMS timestamps it on submission), so
/// there is no date/time input. Type is "check_in" or "check_out".
/// </summary>
public record LogAttendanceInput(
    string Type,          // check_in | check_out
    string? Location = null,
    string? Comment = null);
