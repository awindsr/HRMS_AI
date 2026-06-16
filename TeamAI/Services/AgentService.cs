using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using TeamAI.Configuration;
using TeamAI.Models.Api;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Chat relay over the EXISTING new-style Foundry agent (Responses protocol). It calls the agent
/// via Azure.AI.Projects; the agent runs its portal-configured instructions + function tools
/// server-side. When the agent requests a function tool, the relay runs it in-process (under the
/// signed-in user's token) and submits the output back, looping until the agent returns its final
/// answer. Conversation continuity uses the Responses API's PreviousResponseId — the relay returns
/// each response id as the "threadId".
///
/// Auth is Entra ID (the Foundry agents API does not accept an API key); on a personal-account
/// dev box this is an interactive browser sign-in (cached after the first prompt).
/// </summary>
public sealed class AgentService : IAgentService
{
    // Safety cap on tool round-trips per turn, so a misbehaving agent can't loop forever.
    private const int MaxToolHops = 6;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // The HRMS tools, declared inline on every request (the new-style agent's portal has no
    // client-side function-tool option; OpenAPI/MCP/A2A are all sessionless callbacks). Declaring
    // them here keeps execution in our backend, under the signed-in user's token. Names match the
    // old OpenAPI operationIds so the agent's instructions need no change.
    private static readonly IReadOnlyList<ResponseTool> HrmsTools = new[]
    {
        ResponseTool.CreateFunctionTool(
            functionName: AgentToolDispatcher.GetTeamAttendance,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "date": { "type": "string", "description": "The lookup date in YYYY-MM-DD format." },
                    "team_id": { "type": "integer", "description": "Team identifier. Omit to use the default team." }
                  },
                  "required": ["date"]
                }
                """),
            strictModeEnabled: false,
            functionDescription: "Get a team's attendance for a single date: a summary plus per-member status "
                + "(present, absent, leave, weekly_off), check-in/out times, worked/break hours, shift times, and "
                + "leave details. Read-only. Results reflect the signed-in user's HRMS permissions."),

        ResponseTool.CreateFunctionTool(
            functionName: AgentToolDispatcher.LogAttendance,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "employee_code": { "type": "string", "description": "The member's employee code, as returned by getTeamAttendance." },
                    "type": { "type": "string", "enum": ["check_in", "check_out"] },
                    "team_id": { "type": "integer", "description": "Optional team id; omit to use the default team." },
                    "location": { "type": "string" },
                    "comment": { "type": "string" }
                  },
                  "required": ["employee_code", "type"]
                }
                """),
            strictModeEnabled: false,
            functionDescription: "Record a LIVE check-in or check-out for one team member AT THE CURRENT TIME. HRMS "
                + "timestamps the punch on submission, so there is no date/time and it cannot be backdated. ALWAYS "
                + "confirm with the user before calling."),
    };

    private readonly AgentOptions _options;
    private readonly ILogger<AgentService> _logger;
    private readonly ProjectResponsesClient? _responses;

    public AgentService(IOptions<AgentOptions> options, ILogger<AgentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Agent section is not configured (Agent:ConnectionString empty). The chat relay is disabled.");
            return;
        }

        var projectClient = new AIProjectClient(new Uri(_options.ConnectionString), BuildCredential());
        var agentRef = new AgentReference(name: _options.AgentName, version: _options.AgentVersion);
        _responses = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentRef);
    }

    private Azure.Core.TokenCredential BuildCredential()
    {
        // Dev with an MFA-enforced tenant: interactive browser sign-in, token cached on disk so the
        // prompt happens once. Otherwise DefaultAzureCredential (managed identity in prod, etc.).
        if (_options.InteractiveLogin)
        {
            _logger.LogInformation("Using interactive browser sign-in for the Foundry agent (dev).");
            return new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = string.IsNullOrWhiteSpace(_options.TenantId) ? null : _options.TenantId,
                TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = "TeamAI.AgentRelay" },
            });
        }

        var credOptions = new DefaultAzureCredentialOptions { AdditionallyAllowedTenants = { "*" } };
        if (!string.IsNullOrWhiteSpace(_options.TenantId))
            credOptions.TenantId = _options.TenantId;
        return new DefaultAzureCredential(credOptions);
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        if (_responses is not null)
            _logger.LogInformation("Chat relay ready (agent '{Name}' v{Version}).", _options.AgentName, _options.AgentVersion);
        return Task.CompletedTask;
    }

    public async Task<ChatResponse> SendMessageAsync(
        string? threadId, string message,
        Func<string, string, CancellationToken, Task<string>> toolDispatcher,
        CancellationToken ct = default)
    {
        if (_responses is null)
            return Failed(threadId ?? "", "upstream_unavailable", "The chat service is not configured.", "unavailable");

        try
        {
            // Run the agent with the HRMS tools declared inline. Passing the prior response id
            // chains the turn so the agent keeps conversation context (needed for confirm → "yes"
            // flows like attendance logging).
            var previousResponseId = string.IsNullOrWhiteSpace(threadId) ? null : threadId;
            var options = BuildOptions(previousResponseId);
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(message));
            ResponseResult response = await _responses.CreateResponseAsync(options, ct);

            // Function-tool loop: while the agent emits tool calls, execute them in-process (under
            // the signed-in user's token via the dispatcher) and feed the outputs back, until it
            // returns a final answer instead of more tool calls.
            for (var hop = 0; hop < MaxToolHops; hop++)
            {
                var calls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();
                if (calls.Count == 0)
                    break;

                var next = BuildOptions(response.Id);
                foreach (var call in calls)
                {
                    var args = call.FunctionArguments?.ToString() ?? "{}";
                    var output = await toolDispatcher(call.FunctionName, args, ct);
                    next.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId, output));
                }

                response = await _responses.CreateResponseAsync(next, ct);
            }

            var reply = response.GetOutputText() ?? string.Empty;
            var newThreadId = string.IsNullOrEmpty(response.Id) ? (threadId ?? "") : response.Id;
            return new ChatResponse(newThreadId, reply, Array.Empty<ToolCallInfo>(), "completed", Error: null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Azure.Identity.AuthenticationFailedException ex)
        {
            _logger.LogWarning(ex, "Foundry sign-in failed.");
            return Failed(threadId ?? "", "upstream_unavailable", "Sign-in to the assistant service is required.", "failed");
        }
        catch (System.ClientModel.ClientResultException ex)
        {
            _logger.LogWarning(ex, "Foundry Responses call failed ({Status}).", ex.Status);
            var (code, errMessage) = ex.Status switch
            {
                401 or 403 => ("upstream_unavailable", "The assistant service could not complete the request."),
                429 => ("rate_limited", "The assistant is busy right now (model rate limit). Please wait a few seconds and try again."),
                _ => ("agent_run_failed", "The assistant service could not complete the request."),
            };
            return Failed(threadId ?? "", code, errMessage, "failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in chat turn.");
            return Failed(threadId ?? "", "agent_run_failed", "The assistant could not complete the request.", "failed");
        }
    }

    public async IAsyncEnumerable<AgentStreamEvent> StreamMessageAsync(
        string? threadId, string message,
        Func<string, string, CancellationToken, Task<string>> toolDispatcher,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // The Responses-for-agent client returns the final answer in one call (after the tool
        // loop), so we run the turn and emit it as: thread → delta (full reply) → done (or error).
        // The SSE contract is unchanged, so the frontend needs no changes.
        var result = await SendMessageAsync(threadId, message, toolDispatcher, ct);

        yield return new AgentStreamEvent("thread", result.ThreadId);

        if (result.Error is not null)
        {
            yield return ErrorEvent(result.Error.Code, result.Error.Message);
            yield break;
        }

        if (!string.IsNullOrEmpty(result.Reply))
            yield return new AgentStreamEvent("delta", JsonSerializer.Serialize(new { text = result.Reply }, Json));

        yield return new AgentStreamEvent("done", "[DONE]");
    }

    // A request carrying the inline HRMS tools, optionally chained to a prior response. Tools and
    // InputItems are get-only collections on the options, so they're populated rather than assigned.
    private static CreateResponseOptions BuildOptions(string? previousResponseId)
    {
        var options = new CreateResponseOptions();
        if (!string.IsNullOrWhiteSpace(previousResponseId))
            options.PreviousResponseId = previousResponseId;
        foreach (var tool in HrmsTools)
            options.Tools.Add(tool);
        return options;
    }

    private static ChatResponse Failed(string threadId, string code, string message, string finishReason) =>
        new(threadId, string.Empty, Array.Empty<ToolCallInfo>(), finishReason, new ApiError(code, message));

    private static AgentStreamEvent ErrorEvent(string code, string message) =>
        new("error", JsonSerializer.Serialize(new ApiError(code, message), Json));
}
