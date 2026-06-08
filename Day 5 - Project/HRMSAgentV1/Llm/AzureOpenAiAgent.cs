using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using HrmsAgent.Tools;
using OpenAI.Chat;

namespace HrmsAgent.Llm;

/// <summary>
/// The Azure OpenAI function-calling loop: send the conversation + tool definitions, and
/// while the model asks for tool calls, run each tool via the dispatcher, append the
/// results, and re-send. Stop when the model returns a normal text answer.
/// </summary>
public sealed class AzureOpenAiAgent
{
    private readonly ChatClient _chat;
    private readonly HrmsTools _tools;
    private readonly List<ChatMessage> _messages;

    public AzureOpenAiAgent(string endpoint, string apiKey, string deployment, string systemPrompt, HrmsTools tools)
    {
        var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _chat = client.GetChatClient(deployment);
        _tools = tools;
        _messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
    }

    public async Task<string> AskAsync(string userMessage)
    {
        _messages.Add(new UserChatMessage(userMessage));

        var options = new ChatCompletionOptions();
        foreach (var tool in ToolSchemas.All) options.Tools.Add(tool);

        while (true)
        {
            ChatCompletion completion = await _chat.CompleteChatAsync(_messages, options);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                _messages.Add(new AssistantChatMessage(completion)); // record the tool-call request
                foreach (var call in completion.ToolCalls)
                {
                    var result = await DispatchAsync(call.FunctionName, call.FunctionArguments);
                    _messages.Add(new ToolChatMessage(call.Id, result));
                }
                continue; // loop again so the model can use the results
            }

            var answer = completion.Content.Count > 0 ? completion.Content[0].Text : "";
            _messages.Add(new AssistantChatMessage(answer));
            return answer;
        }
    }

    private async Task<string> DispatchAsync(string name, BinaryData argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var root = doc.RootElement;

        string? Str(string k) => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        int? Int(string k) => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
        bool Bool(string k) => root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;

        return name switch
        {
            // reads (Day 5)
            "getEmployeeList"    => await _tools.GetEmployeeListAsync(Str("department"), Str("status"), Int("limit")),
            "getEmployeeDetails" => await _tools.GetEmployeeDetailsAsync(Str("employeeId") ?? ""),
            "getTaskList"        => await _tools.GetTaskListAsync(Str("employeeId"), Str("status")),
            "getTaskDetails"     => await _tools.GetTaskDetailsAsync(Str("taskId") ?? ""),
            // writes (Day 6) — confirmation enforced inside each tool
            "createTask"     => await _tools.CreateTaskAsync(Str("title") ?? "", Str("description"), Str("assigneeId"), Str("priority"), Str("dueDate"), Bool("confirmed")),
            "assignTask"     => await _tools.AssignTaskAsync(Str("taskId") ?? "", Str("assigneeId") ?? "", Bool("confirmed")),
            "markAttendance" => await _tools.MarkAttendanceAsync(Str("employeeId") ?? "", Str("date"), Str("status") ?? "", Str("checkIn"), Str("checkOut"), Str("note"), Bool("confirmed")),
            "deleteTask"     => await _tools.DeleteTaskAsync(Str("taskId") ?? "", Str("confirmationToken")),
            _ => JsonSerializer.Serialize(new { error = "unknown_tool", message = $"No tool named {name}." })
        };
    }
}
