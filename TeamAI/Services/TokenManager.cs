using Microsoft.AspNetCore.Authentication;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Resolves the HRMS bearer token for an outgoing call: the signed-in user's HRMS JWT, read from
/// the encrypted auth cookie — so HRMS authorization applies per signed-in user. Every HRMS call
/// now originates from an authenticated request (the chat relay runs the agent's tools in-process,
/// and /me is browser-authenticated), so there is no service-token fallback. Registered Scoped;
/// never logs the token.
/// </summary>
public sealed class TokenManager : ITokenManager
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenManager(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated == true)
        {
            var userToken = await http.GetTokenAsync(AuthConstants.HrmsTokenName);
            if (!string.IsNullOrWhiteSpace(userToken))
                return userToken;
        }

        throw new InvalidOperationException(
            "No HRMS token available: the request is not signed in. HRMS calls require an authenticated user session.");
    }
}
