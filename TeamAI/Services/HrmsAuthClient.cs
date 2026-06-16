using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamAI.Models.Api;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Calls the HRMS External Authentication token endpoint to turn a user's credentials into a
/// short-lived JWT. HRMS errors (<c>invalid_grant</c>, <c>user_locked</c>, <c>rate_limited</c>, …)
/// are mapped onto <see cref="HrmsAuthResult"/> with the upstream status preserved. The username
/// is logged for traceability; the password and token never are.
/// </summary>
public sealed class HrmsAuthClient : IHrmsAuthClient
{
    private const string HrmsClientName = "VoyonFolks";

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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/external-auth/token")
        {
            Content = JsonContent.Create(new { username, password, offset = offsetMinutes }),
        };

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("HRMS token endpoint timed out for user '{User}'.", username);
            return HrmsAuthResult.Fail(StatusCodes.Status504GatewayTimeout,
                "server_error", "The sign-in service did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach the HRMS token endpoint for user '{User}'.", username);
            return HrmsAuthResult.Fail(StatusCodes.Status502BadGateway,
                "server_error", "Could not reach the sign-in service.");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var token = JsonSerializer.Deserialize<TokenResponse>(body, JsonOpts);
                    if (token is not null && !string.IsNullOrWhiteSpace(token.access_token))
                    {
                        _logger.LogInformation("HRMS sign-in succeeded for user '{User}'.", username);
                        return HrmsAuthResult.Ok(token.access_token,
                            token.expires_in > 0 ? token.expires_in : 7200);
                    }
                }
                catch (JsonException) { /* fall through to error below */ }

                _logger.LogWarning("HRMS token endpoint returned an unreadable success body.");
                return HrmsAuthResult.Fail(StatusCodes.Status502BadGateway,
                    "server_error", "The sign-in service returned an unexpected response.");
            }

            // Error: HRMS uses { "error": "...", "error_description": "..." }.
            var (error, description) = ParseError(body);
            _logger.LogInformation(
                "HRMS sign-in failed for user '{User}' ({Status} {Error}).",
                username, (int)response.StatusCode, error);

            return HrmsAuthResult.Fail((int)response.StatusCode, error, description);
        }
    }

    private static (string error, string description) ParseError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var error = root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String
                ? e.GetString() : null;
            var desc = root.TryGetProperty("error_description", out var d) && d.ValueKind == JsonValueKind.String
                ? d.GetString() : null;
            return (error ?? "server_error", desc ?? "Sign-in failed.");
        }
        catch (JsonException)
        {
            return ("server_error", "Sign-in failed.");
        }
    }

    // Matches the HRMS success body shape; lower-case to map the JSON without attributes.
    private sealed record TokenResponse(string? access_token, string? token_type, int expires_in);
}
