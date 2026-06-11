namespace TeamAI.Models.Tools;

/// <summary>Roll-up counts for the team on the requested date.</summary>
public record AttendanceSummary(
    int Total, int Present, int Absent, int OnLeave, int OnWeeklyOff, int NotReported);
