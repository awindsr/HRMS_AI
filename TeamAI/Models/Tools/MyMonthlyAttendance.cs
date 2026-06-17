namespace TeamAI.Models.Tools;

/// <summary>
/// The clean, agent-facing view of the SIGNED-IN user's own attendance for one month — the output
/// of the <c>getMyMonthlyAttendance</c> tool: roll-up counts plus a per-day list.
/// </summary>
public record MyMonthlyAttendance(
    int Month,
    int Year,
    MonthlySummary Summary,
    List<MyMonthlyDay> Days);

/// <summary>Month roll-up day counts (decimals allow half-days).</summary>
public record MonthlySummary(
    decimal Offered,
    decimal Present,
    decimal Absent,
    decimal Leave,
    decimal Holiday,
    decimal WeeklyOff);

/// <summary>One day in the month. Times are 24-hour "HH:mm"; null when not recorded.</summary>
public record MyMonthlyDay(
    string Date,
    string Status,
    string? FirstCheckIn,
    string? LastCheckOut,
    string? WorkedHours);
