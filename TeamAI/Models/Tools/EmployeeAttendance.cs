namespace TeamAI.Models.Tools;

/// <summary>
/// One member's attendance, cleaned for the agent. <c>Status</c> is one of
/// present | absent | leave | weekly_off. HRMS quirks (e.g. "--:--" hours) are normalized
/// to null. Leave-specific fields are populated only when the member is on leave. No photos,
/// coordinates, or internal ids are present.
/// </summary>
public record EmployeeAttendance(
    string Name, string EmployeeCode, string Status,
    string? CheckInTime, string? CheckOutTime,
    string? WorkedHours, string? BreakHours,
    string ShiftStart, string ShiftEnd,
    string? LeaveType, string? LeaveReason,
    string? LeaveStartDate, string? LeaveToDate, string? LeaveHours);
