using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TeamAI.Services;
using TeamAI.Services.Interfaces;

namespace TeamAI.Controllers;

/// <summary>
/// The OpenAPI tool surface that Foundry calls. One read-only endpoint returns a team's
/// attendance for a date; errors come back as { error: { code, message } } so the model can
/// read and explain them. The HRMS token is never echoed in any response.
/// </summary>
[ApiController]
[Route("api/v1/tools")]
public sealed class ToolsController : ControllerBase
{
    private readonly IAttendanceService _attendance;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(IAttendanceService attendance, ILogger<ToolsController> logger)
    {
        _attendance = attendance;
        _logger = logger;
    }

    /// <summary>GET /api/v1/tools/team-attendance?date=YYYY-MM-DD&amp;team_id=34110</summary>
    [HttpGet("team-attendance")]
    public async Task<IActionResult> GetTeamAttendance(
        [FromQuery] string? date,
        [FromQuery(Name = "team_id")] int? teamId,
        CancellationToken ct)
    {
        if (!TryParseDate(date, out var parsedDate))
            return ToolError("invalid_date",
                "The 'date' query parameter is required and must be in YYYY-MM-DD format.");

        try
        {
            var result = await _attendance.GetTeamAttendanceAsync(parsedDate, teamId, ct);
            return Ok(result);
        }
        catch (HrmsUnauthorizedException ex)
        {
            _logger.LogWarning("HRMS unauthorized: {Message}", ex.Message);
            return ToolError("hrms_unauthorized", "The HR system rejected the request credentials.");
        }
        catch (HrmsUnavailableException ex)
        {
            _logger.LogWarning(ex, "HRMS unavailable: {Message}", ex.Message);
            return ToolError("hrms_unavailable", "The HR system is currently unavailable. Please try again shortly.");
        }
    }

    public sealed record LogAttendanceRequest(
        string? EmployeeCode, string? Type, int? TeamId, string? Location, string? Comment);

    /// <summary>
    /// POST /api/v1/tools/log-attendance — records a LIVE check-in/check-out for a team member
    /// at the current time. HRMS timestamps the punch on submission; there is no date/time input.
    /// </summary>
    [HttpPost("log-attendance")]
    public async Task<IActionResult> LogAttendance([FromBody] LogAttendanceRequest? body, CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.EmployeeCode))
            return ToolError("invalid_argument", "'employeeCode' is required.");

        var type = (body.Type ?? "").Trim().ToLowerInvariant();
        if (type != "check_in" && type != "check_out")
            return ToolError("invalid_type", "'type' must be 'check_in' or 'check_out'.");

        try
        {
            var input = new TeamAI.Models.Tools.LogAttendanceInput(
                body.EmployeeCode!, type, body.TeamId, body.Location, body.Comment);
            var result = await _attendance.LogAttendanceAsync(input, ct);
            return Ok(result);
        }
        catch (HrmsUnauthorizedException ex)
        {
            _logger.LogWarning("HRMS unauthorized (log): {Message}", ex.Message);
            return ToolError("hrms_unauthorized", "The HR system rejected the request credentials.");
        }
        catch (HrmsUnavailableException ex)
        {
            _logger.LogWarning(ex, "HRMS unavailable (log): {Message}", ex.Message);
            return ToolError("hrms_unavailable", "The HR system is currently unavailable. Please try again shortly.");
        }
    }

    /// <summary>Strict YYYY-MM-DD parse; rejects nulls, times, and locale-dependent forms.</summary>
    private static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(value)
            && DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);
    }

    // Errors return HTTP 200 with an { error: { code, message } } body. Foundry's OpenAPI tool
    // runtime treats any non-2xx as a hard tool failure (failing the whole agent run), so a 200
    // lets the error reach the agent, which then explains it to the user per its instructions.
    private IActionResult ToolError(string code, string message) =>
        Ok(new { error = new { code, message } });
}
