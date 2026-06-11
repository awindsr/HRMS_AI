namespace TeamAI.Models.Api;

/// <summary>
/// Result of one non-streaming chat turn. <see cref="Error"/> is null on success; on failure
/// it carries the code/message and <see cref="FinishReason"/> reflects the run's terminal state.
/// </summary>
public record ChatResponse(
    string ThreadId,
    string Reply,
    IReadOnlyList<ToolCallInfo> ToolCalls,
    string FinishReason,
    ApiError? Error);
