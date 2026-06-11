using Microsoft.AspNetCore.Mvc;
using TeamAI.Models.Api;
using TeamAI.Services;
using TeamAI.Services.Interfaces;

namespace TeamAI.Controllers;

/// <summary>
/// Returns the signed-in manager's display identity, decoded from the server-held HRMS token.
/// Used by the UI greeting. The token never leaves the backend; only name/email/employeeId are
/// returned. There is no login, so "me" is whoever the configured token represents.
/// </summary>
[ApiController]
[Route("api/v1/me")]
public sealed class MeController : ControllerBase
{
    private readonly ITokenManager _tokens;

    public MeController(ITokenManager tokens) => _tokens = tokens;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var token = await _tokens.GetTokenAsync(ct);
            return Ok(JwtReader.ReadProfile(token));
        }
        catch (InvalidOperationException)
        {
            // Token not configured — return an empty profile so the UI falls back gracefully.
            return Ok(new UserProfile(null, null, null));
        }
    }
}
