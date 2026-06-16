namespace TeamAI.Configuration;

/// <summary>
/// Bound from the "Hrms" configuration section. Non-secret upstream settings only: the per-user
/// HRMS bearer token is no longer configured here — it is resolved per request from the signed-in
/// user's session by <see cref="TeamAI.Services.TokenManager"/>.
/// </summary>
public class HrmsOptions
{
    public const string Section = "Hrms";

    public string BaseUrl { get; set; } = "";
    public int DefaultTeamId { get; set; }
    public string DefaultReportingType { get; set; } = "1";
}
