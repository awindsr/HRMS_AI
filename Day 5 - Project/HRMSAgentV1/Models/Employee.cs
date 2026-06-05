using System.Text.Json.Serialization;

namespace HrmsAgent.Models;

public record Employee
{
    [JsonPropertyName("employeeId")] public string EmployeeId { get; init; } = "";
    [JsonPropertyName("fullName")]   public string FullName { get; init; } = "";
    [JsonPropertyName("email")]      public string Email { get; init; } = "";
    [JsonPropertyName("jobTitle")]   public string JobTitle { get; init; } = "";
    [JsonPropertyName("department")] public string Department { get; init; } = "";
    [JsonPropertyName("managerId")]  public string? ManagerId { get; init; }
    [JsonPropertyName("location")]   public string Location { get; init; } = "";
    [JsonPropertyName("status")]     public string Status { get; init; } = "active"; // active | on_leave | inactive
    [JsonPropertyName("joinDate")]   public DateOnly JoinDate { get; init; } = DateOnly.FromDateTime(DateTime.Now);
}