using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using HrmsAgent.Logging;

namespace HrmsAgent.Tools;

/// <summary>
/// The API wrapper. Owns the HttpClient, applies configuration (base URL, key header,
/// timeout), performs the request, handles every failure mode explicitly, logs the call,
/// and returns a small <see cref="ApiResult{T}"/> so callers never touch exceptions.
///
/// Day 5 added GET (the read tools). Day 6 adds POST/PATCH/DELETE (the write tools) — all
/// four verbs share one <see cref="SendAsync{T}"/> core so the error taxonomy and logging
/// are identical no matter the operation.
/// </summary>
public sealed class HrmsApiClient
{
    private readonly HttpClient _http;
    private readonly ApiLogger _logger;

    public HrmsApiClient(string baseUrl, string apiKey, int timeoutSeconds, ApiLogger logger)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey); // auth header lives in the wrapper, never in the schema
        _logger = logger;
    }

    /// <summary>GET a relative path and deserialize to T, with full error handling + logging.</summary>
    public Task<ApiResult<T>> GetAsync<T>(string relativePath) =>
        SendAsync<T>(HttpMethod.Get, relativePath);

    /// <summary>POST a JSON body and deserialize the response to T. Used by create-style writes.</summary>
    public Task<ApiResult<T>> PostAsync<T>(string relativePath, object body) =>
        SendAsync<T>(HttpMethod.Post, relativePath, body);

    /// <summary>PATCH a JSON body and deserialize the response to T. Used by update-style writes.</summary>
    public Task<ApiResult<T>> PatchAsync<T>(string relativePath, object body) =>
        SendAsync<T>(HttpMethod.Patch, relativePath, body);

    /// <summary>DELETE a resource and deserialize the response to T. Used by destructive writes.</summary>
    public Task<ApiResult<T>> DeleteAsync<T>(string relativePath) =>
        SendAsync<T>(HttpMethod.Delete, relativePath);

    /// <summary>
    /// One request pipeline for every verb: build the message (with an optional JSON body),
    /// send it, classify the outcome into the stable error taxonomy, log, and wrap in ApiResult.
    /// </summary>
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string relativePath, object? body = null)
    {
        var verb = method.Method; // "GET" | "POST" | "PATCH" | "DELETE" — for the log line
        var sw = Stopwatch.StartNew();
        var url = new Uri(_http.BaseAddress!, relativePath).ToString();
        try
        {
            using var req = new HttpRequestMessage(method, relativePath);
            if (body is not null)
                req.Content = JsonContent.Create(body, options: JsonOpts);

            using var resp = await _http.SendAsync(req);
            sw.Stop();

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogCall(verb, url, 404, sw.ElapsedMilliseconds, "notfound");
                return ApiResult<T>.Fail("not_found", "The requested record does not exist.");
            }

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogCall(verb, url, (int)resp.StatusCode, sw.ElapsedMilliseconds, "http_err",
                    $"HTTP {(int)resp.StatusCode}");
                return ApiResult<T>.Fail("upstream_error", $"HRMS API returned HTTP {(int)resp.StatusCode}.");
            }

            var data = await resp.Content.ReadFromJsonAsync<T>();
            if (data is null)
            {
                _logger.LogCall(verb, url, (int)resp.StatusCode, sw.ElapsedMilliseconds, "empty");
                return ApiResult<T>.Fail("empty_response", "The HRMS API returned no usable data.");
            }

            _logger.LogCall(verb, url, (int)resp.StatusCode, sw.ElapsedMilliseconds, "ok");
            return ApiResult<T>.Success(data);
        }
        catch (TaskCanceledException) // HttpClient.Timeout elapsed
        {
            sw.Stop();
            _logger.LogCall(verb, url, null, sw.ElapsedMilliseconds, "timeout", "request timed out");
            return ApiResult<T>.Fail("timeout", "The HRMS API did not respond in time. Please try again.");
        }
        catch (HttpRequestException ex) // connection refused / DNS / socket
        {
            sw.Stop();
            _logger.LogCall(verb, url, null, sw.ElapsedMilliseconds, "neterr", ex.Message);
            return ApiResult<T>.Fail("network_error", "Could not reach the HRMS API.");
        }
        catch (Exception ex) // last-resort guard: never leak a raw stack trace to the model
        {
            sw.Stop();
            _logger.LogCall(verb, url, null, sw.ElapsedMilliseconds, "error", ex.Message);
            return ApiResult<T>.Fail("unknown_error", "An unexpected error occurred while calling the HRMS API.");
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web);
}
