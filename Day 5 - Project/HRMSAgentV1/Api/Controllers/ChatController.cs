using HrmsAgent.Llm;
using HrmsAgent.Tools;
using Microsoft.AspNetCore.Mvc;

namespace HRMSAgentV1.Api.Controllers;

/// <summary>
/// Web equivalent of the guide's <c>--chat</c> mode: a single turn of the Azure OpenAI
/// function-calling loop. POST a message; the model decides which read tool(s) to call,
/// the tools run against the mock API, and the model's final text answer is returned.
/// Requires AzureOpenAI:Endpoint and AzureOpenAI:ApiKey in configuration.
/// </summary>
[ApiController]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private const string DefaultSystemPrompt =
        "You are the HRMS Assistant. Answer questions about employees and their tasks using ONLY the " +
        "provided read-only tools. Never invent employee data. If a tool returns an error, explain it " +
        "plainly to the user. Keep answers concise.";

    private readonly IConfiguration _config;
    private readonly HrmsTools _tools;

    public ChatController(IConfiguration config, HrmsTools tools)
    {
        _config = config;
        _tools = tools;
    }

    public sealed record ChatRequest(string Message);

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
            return BadRequest(new { error = "missing_message", message = "Provide a non-empty 'message'." });

        var endpoint   = _config["AzureOpenAI:Endpoint"];
        var apiKey     = _config["AzureOpenAI:ApiKey"];
        var deployment = _config["AzureOpenAI:Deployment"] ?? "gpt-4.1-mini";

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(503, new
            {
                error = "llm_not_configured",
                message = "Set AzureOpenAI:Endpoint and AzureOpenAI:ApiKey (appsettings or env vars) to use chat. " +
                          "The tools themselves can be exercised without a key at GET /api/v1/_test."
            });

        // Optional system-prompt.txt override placed next to the binary / content root.
        var promptPath = Path.Combine(AppContext.BaseDirectory, "system-prompt.txt");
        var systemPrompt = System.IO.File.Exists(promptPath)
            ? await System.IO.File.ReadAllTextAsync(promptPath)
            : DefaultSystemPrompt;

        var agent = new AzureOpenAiAgent(endpoint, apiKey, deployment, systemPrompt, _tools);
        var reply = await agent.AskAsync(request.Message);
        return Ok(new { reply });
    }
}
