using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamAI.Models.Hrms;
using TeamAI.Models.Tools;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// The single upstream integration for the SIGNED-IN user's own attendance: calls HRMS for one
/// employee on one date (or month), then reshapes the raw envelope into the clean models the agent
/// consumes. The employee id is ALWAYS the signed-in user's own id, read from their token — the
/// agent can neither see nor choose it. The HRMS token, photo URLs, coordinates, and casing quirks
/// never leave this class.
/// </summary>
public sealed class AttendanceService : IAttendanceService
{
    private const string HrmsClientName = "VoyonFolks";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // PascalCase (no naming policy) so the POST body matches the HRMS model exactly.
    private static readonly JsonSerializerOptions PostJsonOpts = new();

    private readonly IHttpClientFactory _httpFactory;
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IHttpClientFactory httpFactory,
        ITokenManager tokenManager,
        ILogger<AttendanceService> logger)
    {
        _httpFactory = httpFactory;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<MyAttendance> GetMyAttendanceAsync(DateOnly date, CancellationToken ct = default)
    {
        var token = await _tokenManager.GetTokenAsync(ct);
        var employeeId = ResolveEmployeeId(token);
        var dateStr = date.ToString("yyyy-MM-dd");

        var path = $"/m/api/Attendance/AttendanceBasedOnDate?employeeId={employeeId}&date={dateStr}";
        var data = await GetAsync<AttendanceDetailResponse>(path, token, $"attendance on {dateStr}", ct);
        return ReshapeDay(dateStr, data);
    }

    public async Task<MyMonthlyAttendance> GetMyMonthlyAttendanceAsync(int month, int year, CancellationToken ct = default)
    {
        var token = await _tokenManager.GetTokenAsync(ct);
        var employeeId = ResolveEmployeeId(token);

        var path = $"/m/api/Attendance/MonthlyAttendance?employeeId={employeeId}&month={month}&year={year}";
        var data = await GetAsync<MonthlyAttendanceResponse>(path, token, $"monthly attendance {month}/{year}", ct);
        return ReshapeMonth(month, year, data);
    }

    public async Task<LogAttendanceResult> LogAttendanceAsync(LogAttendanceInput input, CancellationToken ct = default)
    {
        var token = await _tokenManager.GetTokenAsync(ct);
        var employeeId = ResolveEmployeeId(token);
        var isCheckIn = string.Equals(input.Type, "check_in", StringComparison.OrdinalIgnoreCase);

        // Live punch: HRMS timestamps the punch at submission, so we record "now". We compute IST
        // (UTC+5:30) for the date/time we send, though HRMS overrides the time itself.
        var istNow = DateTime.UtcNow.AddHours(5.5);
        var today = DateOnly.FromDateTime(istNow);

        var payload = new AttendanceLogRequest
        {
            EmployeeId = employeeId,
            UserName = JwtReader.ReadUserName(token) ?? "",  // HRMS NPEs on a null UserName
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
            ok, input.Type,
            message ?? (ok
                ? $"Recorded your {(isCheckIn ? "check-in" : "check-out")}."
                : "The attendance log was not accepted."));
    }

    /// <summary>Reads the signed-in user's own employee id from the token, or fails the call.</summary>
    private static int ResolveEmployeeId(string token)
    {
        var id = JwtReader.ReadEmployeeId(token);
        if (id is null or 0)
            throw new HrmsUnauthorizedException("The signed-in user's employee id is missing from the token.");
        return id.Value;
    }

    /// <summary>GETs and deserializes an HRMS payload, mapping transport failures to HRMS exceptions.</summary>
    private async Task<T> GetAsync<T>(string path, string token, string what, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(HrmsClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("HRMS request for {What} timed out.", what);
            throw new HrmsUnavailableException("The HRMS API did not respond in time.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HRMS request for {What} failed.", what);
            throw new HrmsUnavailableException("Could not reach the HRMS API.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new HrmsUnauthorizedException("HRMS rejected the user's bearer token.");
            if ((int)response.StatusCode >= 500)
                throw new HrmsUnavailableException($"HRMS returned HTTP {(int)response.StatusCode}.");
            if (!response.IsSuccessStatusCode)
                throw new HrmsUnavailableException($"HRMS returned an unexpected HTTP {(int)response.StatusCode}.");

            try
            {
                var stream = await response.Content.ReadAsStreamAsync(ct);
                var data = await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts, ct);
                if (data is null)
                    throw new HrmsUnavailableException("The HRMS API returned no data.");
                return data;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "HRMS returned a payload for {What} that could not be parsed.", what);
                throw new HrmsUnavailableException("The HRMS API returned an unreadable response.", ex);
            }
        }
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
                throw new HrmsUnauthorizedException("HRMS rejected the user's bearer token.");

            if (!response.IsSuccessStatusCode)
            {
                // Read the upstream body for diagnostics — HRMS often returns a 500 with the actual
                // cause (e.g. a missing field) in the body. Logged server-side only, never surfaced.
                string errorBody;
                try { errorBody = await response.Content.ReadAsStringAsync(ct); }
                catch { errorBody = "<unreadable>"; }
                if (errorBody.Length > 800) errorBody = errorBody[..800] + "…";
                _logger.LogWarning(
                    "HRMS AttendanceLog failed: HTTP {Status} (EmployeeId={EmployeeId}). Body: {Body}",
                    (int)response.StatusCode, payload.EmployeeId, errorBody);
                throw new HrmsUnavailableException($"HRMS returned HTTP {(int)response.StatusCode} for the attendance log.");
            }

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

    // ---- Pure reshaping (raw HRMS -> clean agent models) ----

    private static MyAttendance ReshapeDay(string date, AttendanceDetailResponse data)
    {
        var shift = data.ShiftAndAttendanceLog;
        var summary = data.AttendanceSummary;
        var status = NormalizeStatus(data.Status, data.DayType);

        var punches = (shift?.AttendanceLog ?? new List<AttendanceLogEntry>())
            .Select(p => new AttendancePunch(
                CheckIn: FormatTime(p.CheckInTime),
                CheckOut: FormatTime(p.CheckOutTime),
                CheckInLocation: NullIfBlank(p.CheckInLocation),
                CheckOutLocation: NullIfBlank(p.CheckOutLocation)))
            .Where(p => p.CheckIn is not null || p.CheckOut is not null)
            .ToList();

        var onLeave = status == "leave";
        return new MyAttendance(
            Date: date,
            Status: status,
            DayType: NullIfBlank(data.DayType),
            ShiftStart: NullIfBlank(shift?.ShiftStartTime),
            ShiftEnd: NullIfBlank(shift?.ShiftEndTime),
            WorkedHours: NormalizeHours(summary?.WorkedHours),
            BreakHours: NormalizeHours(summary?.BreakHours),
            LeaveHours: onLeave ? NormalizeHours(summary?.LeaveHours) : null,
            LeaveType: onLeave ? NullIfBlank(data.LeaveTypeName) : null,
            Punches: punches);
    }

    private static MyMonthlyAttendance ReshapeMonth(int month, int year, MonthlyAttendanceResponse data)
    {
        var s = data.AttendanceSummary;
        var summary = new MonthlySummary(
            Offered: s?.Offered ?? 0,
            Present: s?.Present ?? 0,
            Absent: s?.Absent ?? 0,
            Leave: s?.Leave ?? 0,
            Holiday: s?.Holiday ?? 0,
            WeeklyOff: s?.WeeklyOff ?? 0);

        var days = (data.MonthlyAttendance ?? new List<MonthlyAttendanceDay>())
            .Select(d => new MyMonthlyDay(
                Date: d.Date.ToString("yyyy-MM-dd"),
                Status: NormalizeStatus(d.AttendanceStatus, d.DayType),
                FirstCheckIn: NormalizeHours(d.FirstCheckInTime),
                LastCheckOut: NormalizeHours(d.LastCheckOutTime),
                WorkedHours: NormalizeHours(d.TotalWorkedHours)))
            .ToList();

        return new MyMonthlyAttendance(month, year, summary, days);
    }

    // Maps HRMS status/day-type strings onto the agent's fixed vocabulary:
    // present | absent | leave | weekly_off | holiday | not_reported. Unknown values fall through
    // as a lower-cased, underscored token so nothing is silently lost.
    private static string NormalizeStatus(string? status, string? dayType)
    {
        var s = (status ?? "").Trim().ToLowerInvariant().Replace(" ", "");
        return s switch
        {
            "present" or "p" => "present",
            "absent" or "a" => "absent",
            "leave" or "onleave" or "l" => "leave",
            "weeklyoff" or "weeklyholiday" or "wo" => "weekly_off",
            "holiday" or "publicholiday" or "h" => "holiday",
            "notreported" or "notmarked" or "" => NormalizeDayType(dayType),
            _ => s.Replace("/", "_"),
        };
    }

    private static string NormalizeDayType(string? dayType)
    {
        var d = (dayType ?? "").Trim().ToLowerInvariant().Replace(" ", "");
        return d switch
        {
            "weeklyoff" or "weeklyholiday" => "weekly_off",
            "holiday" or "publicholiday" => "holiday",
            "" => "not_reported",
            _ => "not_reported",
        };
    }

    private static string? FormatTime(DateTime? value) =>
        value is { } dt ? dt.ToString("HH:mm", CultureInfo.InvariantCulture) : null;

    // HRMS uses the placeholder "--:--" (and blanks) for missing hours/times; surface as null. A
    // real "00:00" is preserved — it means a genuine zero, not "unavailable".
    private static string? NormalizeHours(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "--:--" ? null : value.Trim();

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
