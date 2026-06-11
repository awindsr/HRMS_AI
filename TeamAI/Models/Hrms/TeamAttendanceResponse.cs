namespace TeamAI.Models.Hrms;

/// <summary>
/// Raw HRMS envelope returned by GET /m/api/Attendance/team-attendance. Internal only —
/// never serialized back to the agent. Deserialized case-insensitively.
/// </summary>
public record TeamAttendanceResponse(
    AttendanceData? Response,
    string? ErrorCode,
    string? LanguageKey,
    string? MessageEN);
