using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using LogsHive.Maui.Models;

namespace LogsHive.Maui.Infrastructure;

/// <summary>
/// Responsible for POSTing <see cref="ErrorPayload"/> to the LogsHive API.
/// </summary>
internal sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _localLogging;

    public ApiClient(LogsHiveOptions options)
    {
        _localLogging = options.EnableLocalLogging;

        var baseUrl = options.Mode == LogsHiveMode.SelfHosted
            ? options.SelfHostedUrl!
            : LogsHiveConstants.SaaSBaseUrl;

        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            _http.DefaultRequestHeaders.Add(LogsHiveConstants.ApiKeyHeader, options.ApiKey);
    }

    public async Task<SendResult> SendAsync(ErrorPayload payload)
    {
        try
        {
            var response = await _http
                .PostAsJsonAsync(LogsHiveConstants.CaptureEndpoint, payload)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                LogLocally($"[LogsHive] Sent successfully ({(int)response.StatusCode}).");
                return SendResult.Sent;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                LogLocally("[LogsHive] 401 Unauthorized — discarding entry.");
                return SendResult.Discard;
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                LogLocally("[LogsHive] 429 Too Many Requests — queuing entry.");
                return SendResult.Queue;
            }

            LogLocally($"[LogsHive] Unexpected status {(int)response.StatusCode} — queuing entry.");
            return SendResult.Queue;
        }
        catch (HttpRequestException ex)
        {
            LogLocally($"[LogsHive] Network error — queuing entry. ({ex.Message})");
            return SendResult.Queue;
        }
        catch (TaskCanceledException)
        {
            LogLocally("[LogsHive] Request timed out — queuing entry.");
            return SendResult.Queue;
        }
        catch (Exception ex)
        {
            LogLocally($"[LogsHive] Unexpected error — queuing entry. ({ex.Message})");
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