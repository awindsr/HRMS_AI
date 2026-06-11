namespace TeamAI.Models.Hrms;

/// <summary>
/// One team member's raw attendance row from HRMS. Contains fields the agent must never see
/// (photo URL, lat/long, internal ids) — these are dropped during the reshape.
/// </summary>
public record EmployeeDetail(
    string EmployeeName, int EmployeeId, string ShortName,
    string ShiftEndTime, string ShiftStartTime, string WorkedHours,
    string BreakHours, string LeaveHours, string? LeaveType, string? LeaveReason,
    bool IsAbsent, bool IsLeave, bool IsWeeklyOff, bool IsPublicHoliday,
    string? HolidayName, string? CheckinTime, string? CheckOutTime,
    string? LeaveStartDate, string? LeaveToDate,
    string? CheckInLatitude, string? CheckInLongitude,
    string? CheckOutLatitude, string? CheckOutLongitude,
    string? CheckInLocation, string? CheckOutLocation,
    string? EmployeePhoto, string? EmployeeCategory, string EmployeeCode);
