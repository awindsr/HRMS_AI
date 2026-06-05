using System.Globalization;

namespace HrmsAgent.Logging;

/// <summary>Structured, append-only logger for outbound HRMS API calls.</summary>
public sealed class ApiLogger
{
    private readonly string _logPath;
    private readonly object _gate = new();

    public ApiLogger(string logDirectory = "logs")
    {
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, "api-calls.log");
    }

    public void LogCall(string method, string url, int? statusCode, long elapsedMs, string outcome, string? error = null)
    {
        var ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var status = statusCode?.ToString() ?? "---";
        var line = $"{ts} | {method,-4} | {status,3} | {elapsedMs,5}ms | {outcome,-7} | {url}"
                 + (error is null ? "" : $" | ERROR: {error}");

        lock (_gate)
        {
            Console.WriteLine($"[API] {line}");
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }
}