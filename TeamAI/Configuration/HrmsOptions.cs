namespace TeamAI.Configuration;

/// <summary>
/// Bound from the "Hrms" configuration section. Non-secret upstream settings only: the per-user
/// HRMS bearer token is no longer configured here — it is resolved per request from the signed-in
/// user's session by <see cref="TeamAI.Services.TokenManager"/>. This is an individual-user
/// assistant: there is no team id — every HRMS call uses the signed-in user's own employee id,
/// read from their token.
/// </summary>
public class HrmsOptions
{
    public const string Section = "Hrms";

    public string BaseUrl { get; set; } = "";
}
