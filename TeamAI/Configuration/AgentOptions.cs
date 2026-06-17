namespace TeamAI.Configuration;

/// <summary>
/// Bound from the "Agent" section. Points the chat relay at a Foundry-deployed MODEL (via the
/// project's Responses API), driven entirely from this backend: we supply the instructions
/// (system-prompt.txt) and the HRMS function tools inline, and execute the tool calls in-process
/// under the signed-in user's token. We do NOT use a named portal agent — a named agent rejects
/// inline tools, which we need for per-user auth. Auth is Entra ID (token provider), never an
/// API key; a personal-account dev box uses interactive sign-in.
/// </summary>
public class AgentOptions
{
    public const string Section = "Agent";

    /// <summary>
    /// The OpenAI v1 endpoint of the Foundry resource, e.g.
    /// https://&lt;resource&gt;.services.ai.azure.com/openai/v1
    /// </summary>
    public string Endpoint { get; set; } = "";

    /// <summary>
    /// The deployed model's name in the Foundry project (Build → Deployments), e.g. "gpt-4.1-mini".
    /// </summary>
    public string ModelDeploymentName { get; set; } = "gpt-4.1-mini";

    /// <summary>
    /// Resource API key (SECRET). When set, it is used to authenticate the model calls. Keep it in
    /// User Secrets / env vars / a gitignored dev file — never in committed appsettings. When empty,
    /// the chat relay falls back to Entra ID (token provider) using <see cref="TenantId"/>.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Entra (Azure AD) tenant id of the Foundry resource. Used only by the Entra fallback (no
    /// ApiKey). Portal → Microsoft Entra ID → Overview → Tenant ID.
    /// </summary>
    public string TenantId { get; set; } = "";

    /// <summary>
    /// Dev only: interactive browser sign-in for the Entra fallback (handles MFA, caches the token)
    /// instead of the default credential chain. Ignored when an ApiKey is set.
    /// </summary>
    public bool InteractiveLogin { get; set; }

    /// <summary>True when an endpoint and a model deployment are configured; gates the chat relay.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ModelDeploymentName);
}
