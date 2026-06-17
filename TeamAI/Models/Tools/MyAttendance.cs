namespace TeamAI.Models.Tools;

/// <summary>
/// The clean, agent-facing view of the SIGNED-IN user's own attendance for one date — the output
/// of the <c>getMyAttendance</c> tool. <c>Status</c> is one of present | absent | leave |
/// weekly_off | holiday. Times are 24-hour "HH:mm" strings; HRMS placeholders are normalized to
/// null. No coordinates, photos, or internal ids are present.
/// </summary>
public record MyAttendance(
    string Date,
    string Status,
    string? DayType,
    string? ShiftStart,
    string? ShiftEnd,
    string? WorkedHours,
    string? BreakHours,
    string? LeaveHours,
    string? LeaveType,
    List<AttendancePunch> Punches);

/// <summary>One check-in/check-out pair from the day's punch log. Times are 24-hour "HH:mm".</summary>
public record AttendancePunch(
    string? CheckIn,
    string? CheckOut,
    string? CheckInLocation,
    string? CheckOutLocation);
