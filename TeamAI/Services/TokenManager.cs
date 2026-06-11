using Microsoft.Extensions.Options;
using TeamAI.Configuration;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// MVP token manager: returns the manually-configured <see cref="HrmsOptions.Token"/>.
/// Registered Scoped so the Phase 2 swap to a per-user JWT is a single-method change.
/// Never logs the token.
/// </summary>
public sealed class TokenManager : ITokenManager
{
    private readonly HrmsOptions _options;

    public TokenManager(IOptions<HrmsOptions> options) => _options = options.Value;

    public Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
            throw new InvalidOperationException(
                "HRMS token is not configured. Set Hrms:Token via user-secrets (dev) or Key Vault (prod).");

        return Task.FromResult(_options.Token);
    }
}
