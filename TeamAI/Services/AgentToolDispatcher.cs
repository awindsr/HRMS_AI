using System.Globalization;
using System.Text.Json;
using TeamAI.Models.Tools;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Maps the agent's function-tool calls onto <see cref="IAttendanceService"/>. Registered Scoped
/// so it shares the request's user token. The function names and argument shapes here must match
/// the function tools configured on the Foundry agent (formerly the hrms_api_tool OpenAPI spec).
/// </summary>
public sealed class AgentToolDispatcher : IAgentToolDispatcher
{
    // Tool names. These match the old OpenAPI operationIds so the agent's existing instructions
    // keep working unchanged; the tools are now declared inline by AgentService (see HrmsTools).
    public const string GetTeamAttendance = "getTeamAttendance";
    public const string LogAttendance = "logAttendance";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IAttendanceService _attendance;
    private readonly ILogger<AgentToolDispatcher> _logger;

    public AgentToolDispatcher(IAttendanceService attendance, ILogger<AgentToolDispatcher> logger)
    {
        _attendance = attendance;
        _logger = logger;
    }

    public async Task<string> InvokeAsync(string name, string argumentsJson, CancellationToken ct = default)
    {
        try
        {
            return name switch
            {
                GetTeamAttendance => await RunGetTeamAttendanceAsync(argumentsJson, ct),
                LogAttendance => await RunLogAttendanceAsync(argumentsJson, ct),
                _ => Error("unknown_tool", $"No tool named '{name}'."),
            };
        }
        catch (JsonException)
        {
            return Error("invalid_argument", "The tool arguments were not valid JSON.");
        }
        catch (HrmsUnauthorizedException ex)
        {
            _logger.LogWarning("HRMS unauthorized ({Tool}): {Message}", name, ex.Message);
            return Error("hrms_unauthorized", "The HR system rejected the request credentials.");
        }
        catch (HrmsUnavailableException ex)
        {
            _logger.LogWarning(ex, "HRMS unavailable ({Tool}): {Message}", name, ex.Message);
            return Error("hrms_unavailable", "The HR system is currently unavailable. Please try again shortly.");
        }
    }

    private async Task<string> RunGetTeamAttendanceAsync(string argsJson, CancellationToken ct)
    {
        using var doc = Parse(argsJson);
        var root = doc.RootElement;

        if (!TryParseDate(GetString(root, "date"), out var date))
            return Error("invalid_date", "The 'date' argument is required and must be in YYYY-MM-DD format.");

        var result = await _attendance.GetTeamAttendanceAsync(date, GetInt(root, "team_id"), ct);
        return JsonSerializer.Serialize(result, Json);
    }

    private async Task<string> RunLogAttendanceAsync(string argsJson, CancellationToken ct)
    {
        using var doc = Parse(argsJson);
        var root = doc.RootElement;

        var employeeCode = GetString(root, "employee_code");
        if (string.IsNullOrWhiteSpace(employeeCode))
            return Error("invalid_argument", "'employee_code' is required.");

        var type = (GetString(root, "type") ?? "").Trim().ToLowerInvariant();
        if (type != "check_in" && type != "check_out")
            return Error("invalid_type", "'type' must be 'check_in' or 'check_out'.");

        var input = new LogAttendanceInput(
            employeeCode!, type, GetInt(root, "team_id"), GetString(root, "location"), GetString(root, "comment"));
        var result = await _attendance.LogAttendanceAsync(input, ct);
        return JsonSerializer.Serialize(result, Json);
    }

    // Tolerate an empty/whitespace argument blob (a no-argument tool call) as an empty object.
    private static JsonDocument Parse(string json) =>
        JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    // Accept the id as a JSON number or a numeric string (models sometimes quote it).
    private static int? GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s)) return s;
        return null;
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(value)
            && DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string Error(string code, string message) =>
        JsonSerializer.Serialize(new { error = new { code, message } }, Json);
}
