namespace TeamAI.Models.Api;

/// <summary>
/// Credentials posted to <c>POST /api/v1/auth/login</c>. They are forwarded once to the HRMS
/// token endpoint to obtain the per-user JWT and are never stored or logged.
/// </summary>
/// <param name="Username">HRMS user id.</param>
/// <param name="Password">HRMS password.</param>
/// <param name="Offset">Optional client timezone offset in minutes (defaults to 0).</param>
public record LoginRequest(string? Username, string? Password, int Offset = 0);
