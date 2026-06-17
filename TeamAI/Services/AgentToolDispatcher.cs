using System.Globalization;
using System.Text.Json;
using TeamAI.Models.Tools;
using TeamAI.Services.Interfaces;

namespace TeamAI.Services;

/// <summary>
/// Maps the agent's function-tool calls onto <see cref="IAttendanceService"/>. Registered Scoped
/// so it shares the request's user token. The function names and argument shapes here must match
/// the function tools configured on the Foundry agent. This is an INDIVIDUAL-user assistant: every
/// tool acts on the signed-in user only — no employee/team identifier is ever accepted from the agent.
/// </summary>
public sealed class AgentToolDispatcher : IAgentToolDispatcher
{
    public const string GetMyAttendance = "getMyAttendance";
    public const string GetMyMonthlyAttendance = "getMyMonthlyAttendance";
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
                GetMyAttendance => await RunGetMyAttendanceAsync(argumentsJson, ct),
                GetMyMonthlyAttendance => await RunGetMyMonthlyAttendanceAsync(argumentsJson, ct),
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

    private async Task<string> RunGetMyAttendanceAsync(string argsJson, CancellationToken ct)
    {
        using var doc = Parse(argsJson);
        var root = doc.RootElement;

        if (!TryParseDate(GetString(root, "date"), out var date))
            return Error("invalid_date", "The 'date' argument is required and must be in YYYY-MM-DD format.");

        var result = await _attendance.GetMyAttendanceAsync(date, ct);
        return JsonSerializer.Serialize(result, Json);
    }

    private async Task<string> RunGetMyMonthlyAttendanceAsync(string argsJson, CancellationToken ct)
    {
        using var doc = Parse(argsJson);
        var root = doc.RootElement;

        var month = GetInt(root, "month");
        var year = GetInt(root, "year");
        if (month is null or < 1 or > 12)
            return Error("invalid_argument", "'month' is required and must be 1-12.");
        if (year is null or < 2000 or > 2100)
            return Error("invalid_argument", "'year' is required and must be a four-digit year.");

        var result = await _attendance.GetMyMonthlyAttendanceAsync(month.Value, year.Value, ct);
        return JsonSerializer.Serialize(result, Json);
    }

    private async Task<string> RunLogAttendanceAsync(string argsJson, CancellationToken ct)
    {
        using var doc = Parse(argsJson);
        var root = doc.RootElement;

        var type = (GetString(root, "type") ?? "").Trim().ToLowerInvariant();
        if (type != "check_in" && type != "check_out")
            return Error("invalid_type", "'type' must be 'check_in' or 'check_out'.");

        var input = new LogAttendanceInput(type, GetString(root, "location"), GetString(root, "comment"));
        var result = await _attendance.LogAttendanceAsync(input, ct);
        return JsonSerializer.Serialize(result, Json);
    }

    // Tolerate an empty/whitespace argument blob (a no-argument tool call) as an empty object.
    private static JsonDocument Parse(string json) =>
        JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    // Accept the value as a JSON number or a numeric string (models sometimes quote it).
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
