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
/// via Azure.AI.Projects; the agent runs its portal-configured instructions + OpenAPI tool
/// (hrms_api_tool) server-side, exactly like the playground. Conversation continuity uses the
/// Responses API's PreviousResponseId — the relay returns each response id as the "threadId".
///
/// Auth is Entra ID (the Foundry agents API does not accept an API key); on a personal-account
/// dev box this is an interactive browser sign-in (cached after the first prompt).
/// </summary>
public sealed class AgentService : IAgentService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

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
                "Agent section is not configured (Agent:ConnectionString empty). The chat relay is disabled; " +
                "the MVP tool endpoints remain available.");
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

    public async Task<ChatResponse> SendMessageAsync(string? threadId, string message, CancellationToken ct = default)
    {
        if (_responses is null)
            return Failed(threadId ?? "", "upstream_unavailable", "The chat service is not configured.", "unavailable");

        try
        {
            // CreateResponse runs the agent (with its server-side OpenAPI tool) and returns the
            // final text. Passing the prior response id chains the turn so the agent keeps
            // conversation context (needed for confirm → "yes" flows like attendance logging).
            ResponseResult response = await Task.Run(() =>
                string.IsNullOrWhiteSpace(threadId)
                    ? _responses.CreateResponse(message)
                    : _responses.CreateResponse(message, threadId), ct);

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
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // The Responses-for-agent client returns the final answer in one call, so we run the turn
        // and emit it as: thread → delta (full reply) → done (or error). The SSE contract is
        // unchanged, so the frontend needs no changes.
        var result = await SendMessageAsync(threadId, message, ct);

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

    private static ChatResponse Failed(string threadId, string code, string message, string finishReason) =>
        new(threadId, string.Empty, Array.Empty<ToolCallInfo>(), finishReason, new ApiError(code, message));

    private static AgentStreamEvent ErrorEvent(string code, string message) =>
        new("error", JsonSerializer.Serialize(new ApiError(code, message), Json));
}
