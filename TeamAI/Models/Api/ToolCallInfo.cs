using System.Text.Json;

namespace TeamAI.Models.Api;

/// <summary>A tool the agent invoked during the run (e.g. getTeamAttendance), with its arguments.</summary>
public record ToolCallInfo(string Name, JsonElement Arguments);
