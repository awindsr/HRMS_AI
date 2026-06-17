using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamAI.Services;
using TeamAI.Services.Interfaces;

namespace TeamAI.Controllers;

/// <summary>
/// Returns the signed-in user's display identity, decoded from their session's HRMS token.
/// Requires an authenticated session — the UI uses a 401 here to know it must show the login
/// screen. The token never leaves the backend; only name/email/employeeId are returned.
/// </summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ITokenManager _tokens;
    private readonly IProfileService _profile;

    public MeController(ITokenManager tokens, IProfileService profile)
    {
        _tokens = tokens;
        _profile = profile;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var token = await _tokens.GetTokenAsync(ct);

        // Employee id from the cookie principal (set at sign-in), falling back to the token claim.
        int? employeeId = int.TryParse(User.FindFirst("employeeId")?.Value, out var id) ? id : null;
        var fallbackName = User.Identity?.Name;

        var profile = await _profile.BuildProfileAsync(token, employeeId, fallbackName, null, ct);
        return Ok(profile);
    }
}
