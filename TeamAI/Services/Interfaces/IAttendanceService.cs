using TeamAI.Models.Tools;

namespace TeamAI.Services.Interfaces;

/// <summary>
/// Calls HRMS for the team on the given date, reshapes the raw response, and returns the
/// clean agent-facing model. Throws <see cref="HrmsUnauthorizedException"/> on a rejected
/// token and <see cref="HrmsUnavailableException"/> when HRMS is unreachable or 5xx.
/// </summary>
public interface IAttendanceService
{
    Task<TeamAttendance> GetTeamAttendanceAsync(
        DateOnly date, int? teamId = null, CancellationToken ct = default);
}
