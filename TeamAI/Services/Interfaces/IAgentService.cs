using TeamAI.Models.Api;

namespace TeamAI.Services.Interfaces;

/// <summary>
/// Drives the EXISTING Foundry agent for the chat relay. The agent runs in Foundry (model +
/// instructions); when it calls a function tool, this service executes that call in-process via
/// the supplied <paramref name="toolDispatcher"/> and feeds the output back, looping until the
/// agent returns a final answer. Running tools here (instead of via a Foundry-side OpenAPI
/// callback) is what lets each HRMS call use the signed-in user's token.
/// </summary>
public interface IAgentService
{
    /// <summary>Resolve and cache the existing agent id once. Safe to call at startup.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// One non-streaming turn. <paramref name="threadId"/> is the prior response id to chain to
    /// (null starts a new thread). <paramref name="toolDispatcher"/> executes a function-tool call
    /// (name, JSON arguments) and returns the JSON result the agent reads back.
    /// </summary>
    Task<ChatResponse> SendMessageAsync(
        string? threadId, string message,
        Func<string, string, CancellationToken, Task<string>> toolDispatcher,
        CancellationToken ct = default);

    /// <summary>Streaming turn for SSE. Yields thread / delta / done / error events in order.</summary>
    IAsyncEnumerable<AgentStreamEvent> StreamMessageAsync(
        string? threadId, string message,
        Func<string, string, CancellationToken, Task<string>> toolDispatcher,
        CancellationToken ct = default);
}
