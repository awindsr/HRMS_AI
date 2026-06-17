namespace TeamAI.Models.Hrms;

/// <summary>
/// Raw HRMS payload from GET /m/api/Attendance/MonthlyAttendance (HRMS
/// <c>SummaryMonthlyAttendanceViewModel</c>) for the signed-in user in one month. Deserialized
/// case-insensitively. Internal only — reshaped before the agent sees it (colour/format fields dropped).
/// </summary>
public sealed record MonthlyAttendanceResponse(
    List<MonthlyAttendanceDay>? MonthlyAttendance,
    MonthlyAttendanceSummary? AttendanceSummary,
    DateTime? JoiningDate,
    string? WeekStartDay);

/// <summary>One calendar day in the month's attendance grid.</summary>
public sealed record MonthlyAttendanceDay(
    DateTime Date,
    string? DayType,
    string? AttendanceStatus,
    string? FirstCheckInTime,
    string? LastCheckOutTime,
    string? TotalWorkedHours,
    bool ForLeave);

/// <summary>Month roll-up day counts (HRMS sends these as decimals to allow half-days).</summary>
public sealed record MonthlyAttendanceSummary(
    decimal Offered,
    decimal Present,
    decimal Leave,
    decimal Absent,
    decimal Holiday,
    decimal WeeklyOff);
