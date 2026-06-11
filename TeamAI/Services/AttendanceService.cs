using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TeamAI.Configuration;
using TeamAI.Models.Hrms;
using TeamAI.Models.Tools;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// The single upstream integration: calls HRMS for one team on one date, then reshapes the
/// raw envelope into the clean <see cref="TeamAttendance"/> the agent consumes. The HRMS
/// token, photo URLs, coordinates, and casing quirks never leave this class.
/// </summary>
public sealed class AttendanceService : IAttendanceService
{
    private const string HrmsClientName = "VoyonFolks";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ITokenManager _tokenManager;
    private readonly HrmsOptions _options;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IHttpClientFactory httpFactory,
        ITokenManager tokenManager,
        IOptions<HrmsOptions> options,
        ILogger<AttendanceService> logger)
    {
        _httpFactory = httpFactory;
        _tokenManager = tokenManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TeamAttendance> GetTeamAttendanceAsync(
        DateOnly date, int? teamId = null, CancellationToken ct = default)
    {
        var resolvedTeamId = teamId ?? _options.DefaultTeamId;
        var dateStr = date.ToString("yyyy-MM-dd");

        // reportingType is a *quoted* string on the wire: "1" -> %221%22. Wrap then escape so
        // the literal quotes survive. Date is date-only (no time component).
        var reportingType = Uri.EscapeDataString($"\"{_options.DefaultReportingType}\"");
        var relativePath =
            $"/m/api/Attendance/team-attendance?employeeId={resolvedTeamId}&date={dateStr}&reportingType={reportingType}";

        var token = await _tokenManager.GetTokenAsync(ct);

        var client = _httpFactory.CreateClient(HrmsClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("HRMS request timed out for team {TeamId} on {Date}.", resolvedTeamId, dateStr);
            throw new HrmsUnavailableException("The HRMS API did not respond in time.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HRMS request failed for team {TeamId} on {Date}.", resolvedTeamId, dateStr);
            throw new HrmsUnavailableException("Could not reach the HRMS API.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new HrmsUnauthorizedException("HRMS rejected the configured bearer token.");

            if ((int)response.StatusCode >= 500)
                throw new HrmsUnavailableException($"HRMS returned HTTP {(int)response.StatusCode}.");

            if (!response.IsSuccessStatusCode)
                throw new HrmsUnavailableException($"HRMS returned an unexpected HTTP {(int)response.StatusCode}.");

            TeamAttendanceResponse? raw;
            try
            {
                var stream = await response.Content.ReadAsStreamAsync(ct);
                raw = await JsonSerializer.DeserializeAsync<TeamAttendanceResponse>(stream, JsonOpts, ct);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "HRMS returned a payload that could not be parsed.");
                throw new HrmsUnavailableException("The HRMS API returned an unreadable response.", ex);
            }

            var data = raw?.Response;
            if (data is null)
                throw new HrmsUnavailableException("The HRMS API returned no attendance data.");

            return Reshape(dateStr, resolvedTeamId, data);
        }
    }

    /// <summary>Pure transformation of raw HRMS data into the clean agent-facing shape.</summary>
    private static TeamAttendance Reshape(string date, int teamId, AttendanceData data)
    {
        var details = data.EmployeeDetails ?? new List<EmployeeDetail>();
        var employees = details.Select(MapEmployee).ToList();

        var summary = new AttendanceSummary(
            Total: employees.Count,
            Present: employees.Count(e => e.Status == "present"),
            Absent: employees.Count(e => e.Status == "absent"),
            OnLeave: data.OnLeave,
            OnWeeklyOff: data.OnWeeklyOff,
            NotReported: data.NotReported);

        return new TeamAttendance(date, teamId, summary, employees);
    }

    private static EmployeeAttendance MapEmployee(EmployeeDetail e)
    {
        var status =
            e.IsLeave ? "leave" :
            e.IsWeeklyOff ? "weekly_off" :
            e.IsAbsent ? "absent" :
            "present";

        return new EmployeeAttendance(
            Name: e.EmployeeName,
            EmployeeCode: e.EmployeeCode,
            Status: status,
            CheckInTime: e.CheckinTime,
            CheckOutTime: e.CheckOutTime,
            WorkedHours: NormalizeHours(e.WorkedHours),
            ShiftStart: e.ShiftStartTime,
            ShiftEnd: e.ShiftEndTime,
            LeaveType: status == "leave" ? e.LeaveType : null);
    }

    // HRMS uses the placeholder "--:--" for an absent member's worked hours; surface it as null.
    private static string? NormalizeHours(string? hours) =>
        string.IsNullOrWhiteSpace(hours) || hours == "--:--" ? null : hours;
}
