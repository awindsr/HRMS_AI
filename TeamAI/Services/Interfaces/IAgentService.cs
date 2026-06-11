using TeamAI.Models.Api;

namespace TeamAI.Services.Interfaces;

/// <summary>
/// Drives the EXISTING Foundry agent's thread/run lifecycle for the chat relay. Tool
/// execution stays Foundry-side (OpenAPI tool → backend tool endpoint); this service never
/// implements function calling or submits tool outputs. The run completes on its own.
/// </summary>
public interface IAgentService
{
    /// <summary>Resolve and cache the existing agent id once. Safe to call at startup.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>One non-streaming turn. Creates the thread when <paramref name="threadId"/> is null.</summary>
    Task<ChatResponse> SendMessageAsync(string? threadId, string message, CancellationToken ct = default);

    /// <summary>Streaming turn for SSE. Yields thread / delta / tool / done / error events in order.</summary>
    IAsyncEnumerable<AgentStreamEvent> StreamMessageAsync(string? threadId, string message, CancellationToken ct = default);
}
