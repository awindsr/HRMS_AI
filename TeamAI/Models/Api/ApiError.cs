namespace TeamAI.Models.Api;

/// <summary>
/// The error envelope returned on every non-2xx chat response and inside an SSE 'error' event.
/// Codes: agent_run_failed, agent_run_expired, bad_request, upstream_unavailable.
/// </summary>
public record ApiError(string Code, string Message);
