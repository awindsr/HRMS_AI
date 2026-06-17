namespace TeamAI.Models.Api;

/// <summary>
/// Outcome of exchanging HRMS credentials for a JWT at the HRMS token endpoint.
/// On success, <see cref="AccessToken"/> and <see cref="ExpiresIn"/> are set. On failure,
/// <see cref="Error"/> / <see cref="ErrorDescription"/> mirror the HRMS error contract
/// (e.g. <c>invalid_grant</c>, <c>user_locked</c>, <c>rate_limited</c>) and
/// <see cref="StatusCode"/> carries the upstream HTTP status to relay to the browser.
/// </summary>
public sealed record HrmsAuthResult(
    bool Success,
    string? AccessToken,
    int ExpiresIn,
    int StatusCode,
    string? Error,
    string? ErrorDescription,
    // Display fields returned directly by m/api/Login/LoginUser (LoginReturnModel), so the cookie
    // principal does not depend on the token's claim spelling. Null on failure.
    string? DisplayName = null,
    int? EmployeeId = null,
    string? PhotoUrl = null)
{
    public static HrmsAuthResult Ok(
        string accessToken, int expiresIn, string? displayName, int? employeeId, string? photoUrl) =>
        new(true, accessToken, expiresIn, 200, null, null, displayName, employeeId, photoUrl);

    public static HrmsAuthResult Fail(int statusCode, string error, string description) =>
        new(false, null, 0, statusCode, error, description);
}
