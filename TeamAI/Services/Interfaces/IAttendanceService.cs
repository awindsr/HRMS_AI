using TeamAI.Models.Tools;

namespace TeamAI.Services.Interfaces;

/// <summary>
/// Calls HRMS for the SIGNED-IN user's own attendance, reshapes the raw response, and returns the
/// clean agent-facing model. The user's employee id is always resolved from their token — never
/// supplied by the agent. Throws <see cref="HrmsUnauthorizedException"/> on a rejected token and
/// <see cref="HrmsUnavailableException"/> when HRMS is unreachable or 5xx.
/// </summary>
public interface IAttendanceService
{
    /// <summary>The signed-in user's attendance for a single date.</summary>
    Task<MyAttendance> GetMyAttendanceAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>The signed-in user's attendance summary for a month (1-12) and year.</summary>
    Task<MyMonthlyAttendance> GetMyMonthlyAttendanceAsync(int month, int year, CancellationToken ct = default);

    /// <summary>
    /// Logs a LIVE check-in/check-out for the signed-in user by posting to HRMS. Throws
    /// <see cref="HrmsUnauthorizedException"/> / <see cref="HrmsUnavailableException"/> for upstream
    /// failures; business outcomes (e.g. rejected) come back in the result.
    /// </summary>
    Task<LogAttendanceResult> LogAttendanceAsync(LogAttendanceInput input, CancellationToken ct = default);
}
