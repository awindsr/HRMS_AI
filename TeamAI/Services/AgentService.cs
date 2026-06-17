using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;
using TeamAI.Configuration;
using TeamAI.Models.Api;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Chat relay over a Foundry-deployed MODEL (Responses protocol), driven entirely from this
/// backend: it supplies the instructions (system-prompt.txt) and the HRMS function tools inline on
/// every request. When the model requests a function tool, the relay runs it in-process (under the
/// signed-in user's token) and submits the output back, looping until the model returns its final
/// answer. Conversation continuity uses the Responses API's PreviousResponseId — the relay returns
/// each response id as the "threadId".
///
/// We call the model directly (not a named portal agent) because a named agent rejects inline
/// tools, and inline tools are what let us execute under the signed-in user's token. Auth is an
/// API key when configured, else Entra ID (token provider) — never an agent-side OpenAPI callback.
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
            functionName: AgentToolDispatcher.GetMyAttendance,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "date": { "type": "string", "description": "The lookup date in YYYY-MM-DD format." }
                  },
                  "required": ["date"]
                }
                """),
            strictModeEnabled: false,
            functionDescription: "Get the SIGNED-IN user's own attendance for a single date: status "
                + "(present, absent, leave, weekly_off, holiday), the day's check-in/out punches, worked/break "
                + "hours, shift times, and leave type. Read-only. Always for the signed-in user — there is no "
                + "employee or team argument."),

        ResponseTool.CreateFunctionTool(
            functionName: AgentToolDispatcher.GetMyMonthlyAttendance,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "month": { "type": "integer", "description": "Calendar month, 1-12." },
                    "year": { "type": "integer", "description": "Four-digit year, e.g. 2026." }
                  },
                  "required": ["month", "year"]
                }
                """),
            strictModeEnabled: false,
            functionDescription: "Get the SIGNED-IN user's own attendance summary for a whole month: roll-up "
                + "counts (present, absent, leave, holiday, weekly off, days offered) plus a per-day list with "
                + "status, first check-in, last check-out, and worked hours. Read-only; for the signed-in user only."),

        ResponseTool.CreateFunctionTool(
            functionName: AgentToolDispatcher.LogAttendance,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "type": { "type": "string", "enum": ["check_in", "check_out"] },
                    "location": { "type": "string" },
                    "comment": { "type": "string" }
                  },
                  "required": ["type"]
                }
                """),
            strictModeEnabled: false,
            functionDescription: "Record a LIVE check-in or check-out for the SIGNED-IN user AT THE CURRENT TIME. "
                + "HRMS timestamps the punch on submission, so there is no date/time and it cannot be backdated. "
                + "It is always for the signed-in user (no employee argument). ALWAYS confirm before calling."),
    };

    private readonly AgentOptions _options;
    private readonly ILogger<AgentService> _logger;
    private readonly ResponsesClient? _responses;
    private readonly string? _instructions;

    public AgentService(IOptions<AgentOptions> options, IHostEnvironment env, ILogger<AgentService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Agent section is not configured (Agent:Endpoint/ModelDeploymentName empty). The chat relay is disabled.");
            return;
        }

        _instructions = LoadInstructions(env);

        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(_options.Endpoint) };
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("Chat relay using API-key auth for model '{Model}'.", _options.ModelDeploymentName);
            _responses = new ResponsesClient(new ApiKeyCredential(_options.ApiKey), clientOptions);
        }
        else
        {
            _logger.LogInformation("Chat relay using Entra ID auth for model '{Model}'.", _options.ModelDeploymentName);
            var tokenPolicy = new BearerTokenPolicy(BuildCredential(), "https://ai.azure.com/.default");
            _responses = new ResponsesClient(tokenPolicy, clientOptions);
        }
    }

    /// <summary>Loads the system-prompt.txt agent instructions from the content root, if present.</summary>
    private string? LoadInstructions(IHostEnvironment env)
    {
        try
        {
            var path = Path.Combine(env.ContentRootPath, "system-prompt.txt");
            if (File.Exists(path))
                return File.ReadAllText(path);
            _logger.LogWarning("Instructions file not found at {Path}; the model runs without system instructions.", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the instructions file; the model runs without system instructions.");
        }
        return null;
    }

    private Azure.Core.TokenCredential BuildCredential()
    {
        // Dev with an MFA-enforced tenant: interactive browser sign-in, token cached on disk so the
        // prompt happens once. Otherwise DefaultAzureCredential (managed identity in prod, etc.).
        if (_options.InteractiveLogin)
        {
            _logger.LogInformation("Using interactive browser sign-in for the model endpoint (dev).");
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
            _logger.LogInformation("Chat relay ready (model '{Model}').", _options.ModelDeploymentName);
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

    // A request carrying the model, the system instructions, and the inline HRMS tools, optionally
    // chained to a prior response. Tools and InputItems are get-only collections on the options, so
    // they're populated rather than assigned.
    private CreateResponseOptions BuildOptions(string? previousResponseId)
    {
        var options = new CreateResponseOptions { Model = _options.ModelDeploymentName };

        // The model has no inherent knowledge of "now" — left to itself it anchors near its training
        // cutoff (e.g. answering "today" as a 2024 date). Inject the authoritative current IST
        // date/time into the instructions on EVERY turn so all relative-date resolution is correct.
        var instructions = ComposeInstructions();
        if (!string.IsNullOrWhiteSpace(instructions))
            options.Instructions = instructions;

        if (!string.IsNullOrWhiteSpace(previousResponseId))
            options.PreviousResponseId = previousResponseId;
        foreach (var tool in HrmsTools)
            options.Tools.Add(tool);
        return options;
    }

    /// <summary>The static system prompt plus a live "current date/time (IST)" block.</summary>
    private string ComposeInstructions()
    {
        var istNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5));
        var nowBlock =
            "## CURRENT DATE & TIME (authoritative — overrides any other notion of \"now\")\n" +
            $"Right now it is {istNow:dddd, MMMM d, yyyy} at {istNow:h:mm tt} IST (UTC+5:30). " +
            $"Today's date is {istNow:yyyy-MM-dd}. Resolve EVERY relative date or time — \"today\", " +
            "\"yesterday\", \"this month\", \"last week\" — against this exact value, never against your " +
            "training data or any guessed date. The current year is " + istNow.ToString("yyyy") + ".";

        return string.IsNullOrWhiteSpace(_instructions) ? nowBlock : _instructions + "\n\n" + nowBlock;
    }

    private static ChatResponse Failed(string threadId, string code, string message, string finishReason) =>
        new(threadId, string.Empty, Array.Empty<ToolCallInfo>(), finishReason, new ApiError(code, message));

    private static AgentStreamEvent ErrorEvent(string code, string message) =>
        new("error", JsonSerializer.Serialize(new ApiError(code, message), Json));
}
