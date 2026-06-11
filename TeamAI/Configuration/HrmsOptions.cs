namespace TeamAI.Configuration;

/// <summary>
/// Bound from the "Hrms" configuration section. The <see cref="Token"/> is the manual
/// bearer token for the MVP (sourced from user-secrets in dev, Key Vault in prod) — it is a
/// secret and is never committed, logged, or returned to a caller. In Phase 2 the same seam
/// becomes a per-user JWT resolved by the token manager.
/// </summary>
public class HrmsOptions
{
    public const string Section = "Hrms";

    public string BaseUrl { get; set; } = "";
    public int DefaultTeamId { get; set; }
    public string DefaultReportingType { get; set; } = "1";
    public string Token { get; set; } = "";   // MVP manual token; Phase 2 -> per-user JWT
}
