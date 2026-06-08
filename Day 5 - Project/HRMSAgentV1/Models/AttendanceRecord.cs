using System.Text.Json.Serialization;

namespace HrmsAgent.Models;

/// <summary>
/// A single day's attendance entry for one employee. Day 6 write target for markAttendance.
/// Attendance is payroll-grade data: present/absent drives pay, 'leave' decrements balances.
/// </summary>
public record AttendanceRecord
{
    [JsonPropertyName("attendanceId")] public string AttendanceId { get; init; } = "";
    [JsonPropertyName("employeeId")]   public string EmployeeId { get; init; } = "";
    [JsonPropertyName("date")]         public DateOnly Date { get; init; }
    [JsonPropertyName("status")]       public string Status { get; init; } = "present"; // present | absent | wfh | leave | half_day
    [JsonPropertyName("checkIn")]      public string? CheckIn { get; init; }  // HH:mm
    [JsonPropertyName("checkOut")]     public string? CheckOut { get; init; } // HH:mm
    [JsonPropertyName("note")]         public string? Note { get; init; }
    [JsonPropertyName("recordedBy")]   public string RecordedBy { get; init; } = "";
    [JsonPropertyName("recordedAt")]   public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.Now;
}
