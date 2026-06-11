namespace TeamAI.Configuration;

/// <summary>
/// Bound from the "Agent" section. Points the chat relay at the EXISTING Foundry agent
/// (new-style, "Responses protocol") via Azure.AI.Projects. The agent runs its portal-configured
/// instructions + OpenAPI tool (hrms_api_tool) server-side. Auth is Entra ID (the Foundry agents
/// API does not accept an API key); a personal-account dev box uses interactive sign-in.
/// </summary>
public class AgentOptions
{
    public const string Section = "Agent";

    /// <summary>
    /// Foundry project endpoint, e.g.
    /// https://&lt;resource&gt;.services.ai.azure.com/api/projects/&lt;project-name&gt;
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>The agent's name as shown in the portal.</summary>
    public string AgentName { get; set; } = "hrms-agent";

    /// <summary>
    /// The published agent version to call (the portal shows "Version: N"). Update this when you
    /// publish a new version of the agent.
    /// </summary>
    public string AgentVersion { get; set; } = "";

    /// <summary>
    /// Entra (Azure AD) tenant id of the Foundry resource. Required when the dev credential's home
    /// tenant differs from the resource's tenant. Portal → Microsoft Entra ID → Overview → Tenant ID.
    /// </summary>
    public string TenantId { get; set; } = "";

    /// <summary>
    /// Dev only: interactive browser sign-in (handles MFA, caches the token) instead of the default
    /// credential chain. Leave false in production so managed identity is used.
    /// </summary>
    public bool InteractiveLogin { get; set; }

    /// <summary>True when a Foundry project endpoint is configured; gates the chat relay.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
