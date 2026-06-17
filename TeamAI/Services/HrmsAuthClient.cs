using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamAI.Models.Api;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Turns a user's credentials into a per-user HRMS JWT via the mobile login endpoint
/// (<c>POST /m/api/Login/LoginUser</c>). This is the same login the HRMS mobile app uses, so the
/// issued token carries the standard per-user claims (EmployeeId, CompanyId, TimezoneOffset, …)
/// that the <c>m/api/*</c> endpoints expect. Failures (locked, invalid user, password policy) are
/// mapped onto <see cref="HrmsAuthResult"/> with the upstream status preserved. The username is
/// logged for traceability; the password and token never are.
/// </summary>
public sealed class HrmsAuthClient : IHrmsAuthClient
{
    private const string HrmsClientName = "VoyonFolks";

    // Fallback session lifetime when the login response carries no usable interval. The cookie
    // expiry is advisory — an expired HRMS token surfaces as a 401 on the next call regardless.
    private const int DefaultSessionSeconds = 7200;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<HrmsAuthClient> _logger;

    public HrmsAuthClient(IHttpClientFactory httpFactory, ILogger<HrmsAuthClient> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<HrmsAuthResult> AuthenticateAsync(
        string username, string password, int offsetMinutes, CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient(HrmsClientName);

        // LoginResponseModel { UserName, Password, UserDeviceInformation? }. We send only the
        // credentials; device info is optional and unused for a web sign-in.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/m/api/Login/LoginUser")
        {
            Content = JsonContent.Create(new { UserName = username, Password = password }),
        };

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("HRMS login endpoint timed out for user '{User}'.", username);
            return HrmsAuthResult.Fail(StatusCodes.Status504GatewayTimeout,
                "server_error", "The sign-in service did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach the HRMS login endpoint for user '{User}'.", username);
            return HrmsAuthResult.Fail(StatusCodes.Status502BadGateway,
                "server_error", "Could not reach the sign-in service.");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            // 401: LoginController returns { StatusCode, userStatus } for locked / invalid /
            // password-policy. The message is human-readable; relay it as the description.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var status = ReadString(body, "userStatus") ?? "Invalid username or password.";
                _logger.LogInformation("HRMS sign-in denied for user '{User}': {Status}.", username, status);
                return HrmsAuthResult.Fail(StatusCodes.Status401Unauthorized, "invalid_grant", status);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HRMS login endpoint returned HTTP {Status} for user '{User}'.",
                    (int)response.StatusCode, username);
                return HrmsAuthResult.Fail(StatusCodes.Status502BadGateway,
                    "server_error", "The sign-in service returned an unexpected response.");
            }

            LoginReturnModel? login;
            try
            {
                login = JsonSerializer.Deserialize<LoginReturnModel>(body, JsonOpts);
            }
            catch (JsonException)
            {
                login = null;
            }

            // A 200 can still be a logical failure (Status=false / non-200 StatusCode / no token).
            if (login is null || string.IsNullOrWhiteSpace(login.Token) || !login.Status)
            {
                var message = login?.Message ?? "Sign-in failed.";
                _logger.LogInformation(
                    "HRMS sign-in failed for user '{User}' ({Code} {Message}).",
                    username, login?.MessageCode, message);
                return HrmsAuthResult.Fail(StatusCodes.Status401Unauthorized, "invalid_grant", message);
            }

            _logger.LogInformation("HRMS sign-in succeeded for user '{User}'.", username);
            var lifetime = login.Interval is > 0 ? login.Interval.Value * 60 : DefaultSessionSeconds;
            return HrmsAuthResult.Ok(
                login.Token, lifetime,
                displayName: string.IsNullOrWhiteSpace(login.UserName) ? login.ShortName : login.UserName,
                employeeId: login.EmployeeId,
                photoUrl: login.EmployeePhoto);
        }
    }

    private static string? ReadString(string body, string name)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Subset of HRMS LoginReturnModel we consume. Interval is treated as minutes (token refresh
    // interval); verify against the live response if session timing matters.
    private sealed record LoginReturnModel(
        string? Token, bool Status, int StatusCode, string? Message, string? MessageCode,
        int? EmployeeId, int CompanyId, string? UserName, string? ShortName,
        string? EmployeePhoto, int? Interval);
}
