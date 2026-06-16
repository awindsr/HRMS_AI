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

    public MeController(ITokenManager tokens) => _tokens = tokens;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var token = await _tokens.GetTokenAsync(ct);
        return Ok(JwtReader.ReadProfile(token));
    }
}
