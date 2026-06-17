namespace TeamAI.Models.Api;

/// <summary>
/// Display identity for the signed-in user. The basics (name, employee id) come from the login
/// response / token; the richer fields (email, designation, department, photo) are enriched from
/// HRMS GetEmployeeDetails. Only display-friendly fields are exposed — never the token, roles, or
/// access levels. EmployeeId is a string for the frontend; it is the user's OWN id.
/// </summary>
public record UserProfile(
    string? Name,
    string? Email,
    string? EmployeeId,
    string? Designation = null,
    string? Department = null,
    string? BusinessUnit = null,
    string? Company = null,
    string? PhotoUrl = null);
