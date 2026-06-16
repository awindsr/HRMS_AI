using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TeamAI.Models.Api;
using TeamAI.Services;
using TeamAI.Services.Interfaces;

namespace TeamAI.Controllers;

/// <summary>
/// Username/password sign-in for the chat UI. Exchanges HRMS credentials for a per-user JWT and
/// persists it inside an encrypted, httpOnly auth cookie — the browser never sees the token.
/// Subsequent chat / tool calls resolve that user's token via <see cref="TokenManager"/>.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IHrmsAuthClient _auth;

    public AuthController(IHrmsAuthClient auth) => _auth = auth;

    /// <summary>POST /api/v1/auth/login — validate credentials with HRMS and start a session.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? body, CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password))
            return Error(StatusCodes.Status400BadRequest, "invalid_request", "Username and password are required.");

        var result = await _auth.AuthenticateAsync(body.Username, body.Password, body.Offset, ct);
        if (!result.Success || string.IsNullOrWhiteSpace(result.AccessToken))
            return Error(result.StatusCode, result.Error ?? "server_error",
                result.ErrorDescription ?? "Sign-in failed.");

        var profile = JwtReader.ReadProfile(result.AccessToken);

        // Identity claims for the cookie principal (display only); the JWT itself is stored as an
        // auth token so it is encrypted in the cookie and resolvable by TokenManager.
        var claims = new List<Claim> { new(ClaimTypes.Name, profile.Name ?? body.Username) };
        if (!string.IsNullOrWhiteSpace(profile.Email)) claims.Add(new Claim(ClaimTypes.Email, profile.Email));
        if (!string.IsNullOrWhiteSpace(profile.EmployeeId)) claims.Add(new Claim("employeeId", profile.EmployeeId));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            // The HRMS token has a fixed lifetime and no refresh; expire the session with it.
            ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn),
            AllowRefresh = false,
        };
        props.StoreTokens(new[] { new AuthenticationToken { Name = AuthConstants.HrmsTokenName, Value = result.AccessToken } });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        return Ok(profile);
    }

    /// <summary>POST /api/v1/auth/logout — clear the session cookie.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    // Consistent { error: { code, message } } envelope, matching the rest of the API.
    private IActionResult Error(int status, string code, string message) =>
        StatusCode(status, new { error = new ApiError(code, message) });
}
