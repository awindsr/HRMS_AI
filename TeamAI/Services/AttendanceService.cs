using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    // PascalCase (no naming policy) so the POST body matches the HRMS model exactly.
    private static readonly JsonSerializerOptions PostJsonOpts = new();

    public async Task<TeamAttendance> GetTeamAttendanceAsync(
        DateOnly date, int? teamId = null, CancellationToken ct = default)
    {
        var resolvedTeamId = teamId ?? _options.DefaultTeamId;
        var data = await FetchRawAsync(date, resolvedTeamId, ct);
        return Reshape(date.ToString("yyyy-MM-dd"), resolvedTeamId, data);
    }

    public async Task<LogAttendanceResult> LogAttendanceAsync(LogAttendanceInput input, CancellationToken ct = default)
    {
        var resolvedTeamId = input.TeamId ?? _options.DefaultTeamId;
        var isCheckIn = string.Equals(input.Type, "check_in", StringComparison.OrdinalIgnoreCase);

        // Live punch: HRMS timestamps the punch at submission, so we record "now". We compute
        // IST (UTC+5:30, the team timezone) for the date/time we send, though HRMS overrides it.
        var istNow = DateTime.UtcNow.AddHours(5.5);
        var today = DateOnly.FromDateTime(istNow);

        // Resolve the member from the team data so we never trust the agent for the internal id.
        var team = await FetchRawAsync(today, resolvedTeamId, ct);
        var member = (team.EmployeeDetails ?? new List<EmployeeDetail>())
            .FirstOrDefault(e => string.Equals(e.EmployeeCode, input.EmployeeCode, StringComparison.OrdinalIgnoreCase));

        if (member is null)
            return new LogAttendanceResult(false, input.EmployeeCode, "", input.Type,
                $"No team member with code '{input.EmployeeCode}' was found on the team.");

        var token = await _tokenManager.GetTokenAsync(ct);
        var payload = new AttendanceLogRequest
        {
            EmployeeId = member.EmployeeId,
            UserName = member.EmployeeName,                 // best-guess; HRMS field meaning unconfirmed
            CompanyId = JwtReader.ReadCompanyId(token) ?? 0,
            AttendanceDate = today.ToString("yyyy-MM-dd"),
            CheckInCheckOutTime = istNow.ToString("yyyy-MM-dd HH:mm:ss"), // HRMS ignores this; it stamps now
            IsCheckInorCheckOut = isCheckIn ? "CheckIn" : "CheckOut",
            ShiftId = 0, // HRMS rejects null (non-nullable int); 0 = let HRMS resolve the shift
            Location = input.Location,
            Comment = input.Comment,
            IP = null,
        };

        var (ok, message) = await PostAttendanceLogAsync(payload, token, ct);
        return new LogAttendanceResult(
            ok, member.EmployeeCode, member.EmployeeName, input.Type,
            message ?? (ok ? $"Recorded {(isCheckIn ? "check-in" : "check-out")} for {member.EmployeeName}." : "The attendance log was not accepted."));
    }

    /// <summary>POSTs the attendance log to HRMS. Returns (success, optional HRMS message).</summary>
    private async Task<(bool ok, string? message)> PostAttendanceLogAsync(
        AttendanceLogRequest payload, string token, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(HrmsClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/m/api/Attendance/AttendanceLog")
        {
            Content = JsonContent.Create(payload, options: PostJsonOpts),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new HrmsUnavailableException("The HRMS API did not respond in time.", ex);
        }
        catch (HttpRequestException ex)
        {
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

            // HRMS replies { "IsMarked": true, "Message": "Attendance Marked", "StatusCode": 200 }.
            try
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(body)) return (true, null);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var message = root.TryGetProperty("Message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;

                if (root.TryGetProperty("IsMarked", out var im))
                    return (im.ValueKind == JsonValueKind.True, message);

                // Fallback for the older { ErrorCode, MessageEN } envelope.
                var errorCode = root.TryGetProperty("ErrorCode", out var ec) && ec.ValueKind == JsonValueKind.String ? ec.GetString() : null;
                var messageEn = root.TryGetProperty("MessageEN", out var me) && me.ValueKind == JsonValueKind.String ? me.GetString() : null;
                return string.IsNullOrEmpty(errorCode) ? (true, messageEn ?? message) : (false, messageEn ?? $"HRMS error {errorCode}.");
            }
            catch (JsonException)
            {
                // 2xx with a non-JSON body — treat as success.
                return (true, null);
            }
        }
    }

    /// <summary>Fetches and deserializes the raw HRMS team-attendance payload for a date.</summary>
    private async Task<AttendanceData> FetchRawAsync(DateOnly date, int teamId, CancellationToken ct)
    {
        var dateStr = date.ToString("yyyy-MM-dd");

        // reportingType is a *quoted* string on the wire: "1" -> %221%22. Wrap then escape so
        // the literal quotes survive. Date is date-only (no time component).
        var reportingType = Uri.EscapeDataString($"\"{_options.DefaultReportingType}\"");
        var relativePath =
            $"/m/api/Attendance/team-attendance?employeeId={teamId}&date={dateStr}&reportingType={reportingType}";

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
            _logger.LogWarning("HRMS request timed out for team {TeamId} on {Date}.", teamId, dateStr);
            throw new HrmsUnavailableException("The HRMS API did not respond in time.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HRMS request failed for team {TeamId} on {Date}.", teamId, dateStr);
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

            return data;
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

        var onLeave = status == "leave";
        return new EmployeeAttendance(
            Name: e.EmployeeName?.Trim() ?? "",
            EmployeeCode: e.EmployeeCode,
            Status: status,
            CheckInTime: e.CheckinTime,
            CheckOutTime: e.CheckOutTime,
            WorkedHours: NormalizeHours(e.WorkedHours),
            BreakHours: NormalizeHours(e.BreakHours),
            ShiftStart: e.ShiftStartTime,
            ShiftEnd: e.ShiftEndTime,
            // Leave-specific fields only when actually on leave (HRMS leaves them null otherwise).
            LeaveType: onLeave ? e.LeaveType : null,
            LeaveReason: onLeave ? e.LeaveReason : null,
            LeaveStartDate: onLeave ? e.LeaveStartDate : null,
            LeaveToDate: onLeave ? e.LeaveToDate : null,
            LeaveHours: onLeave ? NormalizeHours(e.LeaveHours) : null);
    }

    // HRMS uses the placeholder "--:--" for an absent member's worked hours; surface it as null.
    private static string? NormalizeHours(string? hours) =>
        string.IsNullOrWhiteSpace(hours) || hours == "--:--" ? null : hours;
}
