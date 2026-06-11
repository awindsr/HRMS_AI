namespace TeamAI.Models.Api;

/// <summary>
/// Minimal identity for the signed-in manager, derived server-side from the HRMS token's
/// claims. Only display-friendly fields are exposed — never the token, roles, or access levels.
/// </summary>
public record UserProfile(string? Name, string? Email, string? EmployeeId);
