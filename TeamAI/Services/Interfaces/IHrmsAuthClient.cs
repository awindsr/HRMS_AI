using TeamAI.Models.Api;

namespace TeamAI.Services.Interfaces;

/// <summary>
/// Exchanges HRMS username/password for a per-user JWT via the HRMS External Authentication API
/// (<c>POST /api/external-auth/token</c>). The only place credentials touch the wire; callers
/// receive a token (or a mapped error) and never the raw upstream response.
/// </summary>
public interface IHrmsAuthClient
{
    Task<HrmsAuthResult> AuthenticateAsync(
        string username, string password, int offsetMinutes, CancellationToken ct = default);
}
