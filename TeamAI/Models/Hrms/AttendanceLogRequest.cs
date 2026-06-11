namespace TeamAI.Models.Hrms;

/// <summary>
/// Body for HRMS POST /m/api/Attendance/AttendanceLog (mirrors AttendanceCheckInCheckOutModel).
/// Serialized with PascalCase property names exactly as HRMS expects. Optional fields are sent
/// as null/0 per the HRMS contract. Internal only — never exposed to the agent.
/// </summary>
public sealed class AttendanceLogRequest
{
    public string? Location { get; set; }
    public string? Comment { get; set; }
    public string? IP { get; set; }

    /// <summary>Full date-time of the punch, e.g. "2026-06-11 09:15:00".</summary>
    public string CheckInCheckOutTime { get; set; } = "";
    public string AttendanceDate { get; set; } = "";

    public int EmployeeId { get; set; }
    public string? UserName { get; set; }
    public int CompanyId { get; set; }

    /// <summary>Nullable so it serializes as null (not in the team data).</summary>
    public int? ShiftId { get; set; }

    /// <summary>"CheckIn" or "CheckOut".</summary>
    public string IsCheckInorCheckOut { get; set; } = "";

    public double CheckInLatitude { get; set; }
    public double CheckOutLatitude { get; set; }
    public double CheckInLongitude { get; set; }
    public double CheckOutLongitude { get; set; }
}
