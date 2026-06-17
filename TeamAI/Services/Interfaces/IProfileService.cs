using TeamAI.Models.Api;

namespace TeamAI.Services.Interfaces;

/// <summary>
/// Builds the signed-in user's display profile, enriching the basics (name, employee id) with HRMS
/// employee details (email, designation, department, photo). Enrichment is best-effort: if HRMS is
/// unavailable, a minimal profile from the supplied fallbacks is returned rather than failing —
/// sign-in and session checks must work even when the details endpoint does not.
/// </summary>
public interface IProfileService
{
    Task<UserProfile> BuildProfileAsync(
        string token,
        int? employeeId,
        string? fallbackName,
        string? fallbackPhotoUrl,
        CancellationToken ct = default);
}
