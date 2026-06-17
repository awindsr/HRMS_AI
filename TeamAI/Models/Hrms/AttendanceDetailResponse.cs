namespace TeamAI.Models.Hrms;

/// <summary>
/// Raw HRMS payload from GET /m/api/Attendance/AttendanceBasedOnDate (HRMS
/// <c>AttendanceDetailsViewModel</c>) for the signed-in user on one date. Deserialized
/// case-insensitively. Internal only — reshaped before the agent ever sees it; coordinates and
/// colour codes are dropped in the reshape.
/// </summary>
public sealed record AttendanceDetailResponse(
    string? Status,
    string? DayType,
    string? LeaveTypeName,
    DateTime? LastLogTime,
    string? LastLogLocation,
    string? HolidayGreetingsText,
    ShiftAndAttendanceLog? ShiftAndAttendanceLog,
    AttendanceSummaryDetail? AttendanceSummary);

/// <summary>The day's shift window plus the ordered punch rows.</summary>
public sealed record ShiftAndAttendanceLog(
    string? ShiftStartTime,
    string? ShiftEndTime,
    List<AttendanceLogEntry>? AttendanceLog);

/// <summary>One check-in/check-out punch pair for the day.</summary>
public sealed record AttendanceLogEntry(
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    string? CheckInLocation,
    string? CheckOutLocation);

/// <summary>The day's hour totals (HRMS sends these pre-formatted as "HH:mm" strings).</summary>
public sealed record AttendanceSummaryDetail(
    string? WorkedHours,
    string? ShiftHours,
    string? BreakHours,
    string? LeaveHours,
    string? AnomalyHours);
