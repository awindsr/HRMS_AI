namespace TeamAI.Models.Api;

/// <summary>
/// One Server-Sent Event the relay forwards to the browser.
/// <see cref="Type"/> is the SSE event name: thread | delta | tool | done | error.
/// <see cref="Data"/> is the raw SSE data payload (text or a JSON string).
/// </summary>
public record AgentStreamEvent(string Type, string Data);
