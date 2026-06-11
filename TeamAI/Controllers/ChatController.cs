using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TeamAI.Models.Api;
using TeamAI.Services.Interfaces;

namespace TeamAI.Controllers;

/// <summary>
/// Phase 2 chat relay. Fronts the existing Foundry agent for the custom React UI, alongside
/// the still-working playground. The browser holds no credentials; the HRMS token stays in the
/// backend tool endpoints. Two surfaces: a non-streaming POST and an SSE stream.
/// </summary>
[ApiController]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IAgentService _agent;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IAgentService agent, ILogger<ChatController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>POST /api/v1/chat — one non-streaming turn.</summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest? request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = new ApiError("bad_request", "A non-empty 'message' is required.") });

        var result = await _agent.SendMessageAsync(request.ThreadId, request.Message, ct);

        if (result.Error is not null)
        {
            var status = result.Error.Code switch
            {
                "bad_request" => StatusCodes.Status400BadRequest,
                "agent_run_expired" => StatusCodes.Status504GatewayTimeout,
                "upstream_unavailable" => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status502BadGateway,
            };
            return StatusCode(status, new { error = result.Error });
        }

        return Ok(result);
    }

    /// <summary>GET /api/v1/chat/stream?threadId=&amp;message= — SSE: thread, delta, tool, done, error.</summary>
    [HttpGet("stream")]
    public async Task Stream([FromQuery] string? threadId, [FromQuery] string? message, CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // disable proxy buffering for SSE

        if (string.IsNullOrWhiteSpace(message))
        {
            await WriteEventAsync("error",
                JsonSerializer.Serialize(new ApiError("bad_request", "A non-empty 'message' is required."), Json), ct);
            return;
        }

        try
        {
            await foreach (var ev in _agent.StreamMessageAsync(threadId, message, ct))
                await WriteEventAsync(ev.Type, ev.Data, ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — nothing to do.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while streaming chat.");
            await WriteEventAsync("error",
                JsonSerializer.Serialize(new ApiError("agent_run_failed", "The assistant stream failed."), Json), ct);
        }
    }

    private async Task WriteEventAsync(string eventName, string data, CancellationToken ct)
    {
        // SSE frame: "event: <name>\n" then one "data:" line per line of payload, then a blank line.
        await Response.WriteAsync($"event: {eventName}\n", ct);
        foreach (var line in data.Split('\n'))
            await Response.WriteAsync($"data: {line}\n", ct);
        await Response.WriteAsync("\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
