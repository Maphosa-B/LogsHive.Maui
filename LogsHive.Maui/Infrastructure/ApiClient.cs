using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using LogsHive.Maui.Models;

namespace LogsHive.Maui.Infrastructure;

/// <summary>
/// Responsible for POSTing payloads to the LogsHive API.
/// Uses a single base URL (ResolvedBaseUrl from options) and appends
/// endpoint suffixes — so self-hosted only needs one URL configured.
///
/// Errors:  POST {baseUrl}/errors/capture
/// Memory:  POST {baseUrl}/memory/capture
/// </summary>
internal sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _localLogging;

    // always stored without trailing slash — fullUrl is built as _baseUrl + "/" + endpoint
    private readonly string _baseUrl;

    public ApiClient(LogsHiveOptions options)
    {
        _localLogging = options.EnableLocalConsoleLogging;

        // strip trailing slash once here — all URL building uses _baseUrl + "/" + endpoint
        _baseUrl = options.ResolvedBaseUrl.TrimEnd('/');

        _http = new HttpClient
        {
            // BaseAddress must end with / for HttpClient relative path resolution
            BaseAddress = new Uri(_baseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            _http.DefaultRequestHeaders.Add(LogsHiveConstants.ApiKeyHeader, options.ApiKey);
    }

    // ── Error events ──────────────────────────────────────────────────────────

    public async Task<SendResult> SendAsync(ErrorPayload payload)
        => await PostAsync(LogsHiveConstants.ErrorsCaptureEndpoint, payload).ConfigureAwait(false);

    // ── Memory snapshots ──────────────────────────────────────────────────────

    public async Task<SendResult> SendMemoryAsync(MemoryPayload payload)
        => await PostAsync(LogsHiveConstants.MemoryCaptureEndpoint, payload).ConfigureAwait(false);

    // ── Shared HTTP logic ─────────────────────────────────────────────────────

    private async Task<SendResult> PostAsync<T>(string endpoint, T payload)
    {
        // _baseUrl has no trailing slash, endpoint has no leading slash
        // so this always produces exactly one slash between them
        var fullUrl = $"{_baseUrl}/{endpoint}";

        try
        {
            var response = await _http
                .PostAsJsonAsync(endpoint, payload)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                LogLocally($"[LogsHive] ✓ {(int)response.StatusCode} POST {fullUrl}");
                return SendResult.Sent;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                LogLocally($"[LogsHive] ✗ 401 Unauthorized — POST {fullUrl} — check your ApiKey.");
                return SendResult.Discard;
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                LogLocally($"[LogsHive] ✗ 429 Too Many Requests — POST {fullUrl} — queuing entry.");
                return SendResult.Queue;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                LogLocally($"[LogsHive] ✗ 404 Not Found — POST {fullUrl} — " +
                           $"check your SelfHostedUrl and controller routes.");
                return SendResult.Queue;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                LogLocally($"[LogsHive] ✗ 400 Bad Request — POST {fullUrl} — {body}");
                return SendResult.Discard;
            }

            LogLocally($"[LogsHive] ✗ {(int)response.StatusCode} — POST {fullUrl} — queuing entry.");
            return SendResult.Queue;
        }
        catch (HttpRequestException ex)
        {
            LogLocally($"[LogsHive] ✗ Network error — POST {fullUrl} — {ex.Message} — queuing entry.");
            return SendResult.Queue;
        }
        catch (TaskCanceledException)
        {
            LogLocally($"[LogsHive] ✗ Timeout — POST {fullUrl} — queuing entry.");
            return SendResult.Queue;
        }
        catch (Exception ex)
        {
            LogLocally($"[LogsHive] ✗ Unexpected error — POST {fullUrl} — {ex.Message} — queuing entry.");
            return SendResult.Queue;
        }
    }

    private void LogLocally(string message)
    {
        if (!_localLogging) return;

#if ANDROID
        Android.Util.Log.Debug("[LogsHive]", message);
#else
        Debug.WriteLine(message);
#endif
    }

    public void Dispose() => _http.Dispose();
}

internal enum SendResult
{
    Sent,
    Discard,
    Queue
}