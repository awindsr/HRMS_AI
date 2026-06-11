namespace TeamAI.Models.Tools;

/// <summary>
/// The clean, agent-facing contract returned by GET /api/v1/tools/team-attendance.
/// This is exactly what the OpenAPI tool schema describes.
/// </summary>
public record TeamAttendance(
    string Date, int TeamId, AttendanceSummary Summary, List<EmployeeAttendance> Employees);
