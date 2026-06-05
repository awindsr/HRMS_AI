using System.Text.Json.Serialization;

namespace HrmsAgent.Models;

public record EmployeeTask
{
    [JsonPropertyName("taskId")]      public string TaskId { get; init; } = "";
    [JsonPropertyName("employeeId")]  public string EmployeeId { get; init; } = "";
    [JsonPropertyName("title")]       public string Title { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("status")]      public string Status { get; init; } = "open"; // open | in_progress | done | blocked
    [JsonPropertyName("priority")]    public string Priority { get; init; } = "medium"; // low | medium | high
    [JsonPropertyName("dueDate")]     public DateOnly DueDate { get; init; } = DateOnly.FromDateTime(DateTime.Now);
}