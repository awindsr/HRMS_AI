namespace TeamAI.Services.Interfaces;

/// <summary>
/// Supplies the HRMS bearer token. MVP: returns the manually-configured token.
/// Phase 2 seam: resolve the per-user JWT here without touching callers.
/// </summary>
public interface ITokenManager
{
    Task<string> GetTokenAsync(CancellationToken ct = default);
}
