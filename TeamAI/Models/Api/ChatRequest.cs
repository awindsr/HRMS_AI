namespace TeamAI.Models.Api;

/// <summary>One chat turn from the browser. <see cref="ThreadId"/> is null on the first turn.</summary>
public record ChatRequest(string Message, string? ThreadId);
