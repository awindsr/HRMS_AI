using System.Net.Http.Headers;
using System.Text.Json;
using TeamAI.Models.Api;
using TeamAI.Models.Hrms;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Enriches the signed-in user's display profile from HRMS GET /m/api/Employee/GetEmployeeDetails.
/// Best-effort: any failure (missing id, HRMS down, unreadable body) degrades to a minimal profile
/// built from the supplied fallbacks, so sign-in and session checks never break on enrichment.
/// Never exposes the token; only display fields are surfaced.
/// </summary>
public sealed class HrmsProfileService : IProfileService
{
    private const string HrmsClientName = "VoyonFolks";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<HrmsProfileService> _logger;

    public HrmsProfileService(IHttpClientFactory httpFactory, ILogger<HrmsProfileService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<UserProfile> BuildProfileAsync(
        string token, int? employeeId, string? fallbackName, string? fallbackPhotoUrl, CancellationToken ct = default)
    {
        var id = employeeId ?? JwtReader.ReadEmployeeId(token);
        var minimal = new UserProfile(
            Name: fallbackName,
            Email: null,
            EmployeeId: id?.ToString(),
            PhotoUrl: fallbackPhotoUrl);

        if (id is null or 0)
            return minimal;

        EmployeeProfileResponse? details;
        try
        {
            var client = _httpFactory.CreateClient(HrmsClientName);
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"/m/api/Employee/GetEmployeeDetails?employeeId={id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Profile enrichment skipped: GetEmployeeDetails returned HTTP {Status} for employee {Id}.",
                    (int)response.StatusCode, id);
                return minimal;
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            details = await JsonSerializer.DeserializeAsync<EmployeeProfileResponse>(stream, JsonOpts, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Profile enrichment failed for employee {Id}; using minimal profile.", id);
            return minimal;
        }

        if (details is null)
            return minimal;

        var employment = details.EmploymentDetails;
        var email = NullIfBlank(details.OfficalContactDetails?.OfficalEmailId)
            ?? NullIfBlank(details.PersonalContactDetails?.PersonalEmailId);

        return new UserProfile(
            Name: NullIfBlank(details.FullName) ?? fallbackName,
            Email: email,
            EmployeeId: id.ToString(),
            Designation: NullIfBlank(employment?.Designation),
            Department: NullIfBlank(employment?.Department),
            BusinessUnit: NullIfBlank(employment?.BusinessUnit),
            Company: NullIfBlank(employment?.Company),
            PhotoUrl: NullIfBlank(details.ProfilePhoto) ?? fallbackPhotoUrl);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
