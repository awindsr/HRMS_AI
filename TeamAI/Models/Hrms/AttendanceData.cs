using System.Text.Json.Serialization;

namespace TeamAI.Models.Hrms;

/// <summary>
/// The HRMS attendance payload for one team on one date. Note the camelCase
/// <c>onWeeklyOff</c> quirk in the real JSON — pinned with an explicit attribute.
/// </summary>
public record AttendanceData(
    int ReportedEmployees,
    int OnLeave,
    [property: JsonPropertyName("onWeeklyOff")] int OnWeeklyOff,
    int NotReported,
    List<EmployeeDetail> EmployeeDetails);
