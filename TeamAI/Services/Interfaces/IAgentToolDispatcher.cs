namespace TeamAI.Services.Interfaces;

/// <summary>
/// Executes the Foundry agent's function-tool calls in-process, inside the signed-in user's
/// chat request — so each HRMS call runs under that user's token (resolved by
/// <see cref="ITokenManager"/> from the session cookie), not a shared service token. This
/// replaces the old Foundry-side OpenAPI tool callback, which was a sessionless server-to-server
/// request and therefore could never carry the logged-in user.
/// </summary>
public interface IAgentToolDispatcher
{
    /// <summary>
    /// Runs the named function tool with its JSON arguments and returns the JSON the model reads
    /// back. Errors are returned as <c>{ "error": { "code", "message" } }</c> JSON (never thrown)
    /// so the agent can read and explain them — matching the previous tool-endpoint contract.
    /// </summary>
    Task<string> InvokeAsync(string name, string argumentsJson, CancellationToken ct = default);
}
