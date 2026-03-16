using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using LogsHive.Maui.Models;

namespace LogsHive.Maui.Infrastructure;

/// <summary>
/// Responsible for POSTing <see cref="ErrorPayload"/> to the LogsHive API.
/// Returns a <see cref="SendResult"/> indicating success, discard (401), or queue (429 / no network).
/// </summary>
internal sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly LogsHiveOptions _options;

    public ApiClient(LogsHiveOptions options)
    {
        _options = options;
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            _http.DefaultRequestHeaders.Add(LogsHiveConstants.ApiKeyHeader, options.ApiKey);
    }

    /// <summary>
    /// Sends the payload to the API.
    /// </summary>
    /// <returns>
    /// <see cref="SendResult.Sent"/> on 2xx,
    /// <see cref="SendResult.Discard"/> on 401,
    /// <see cref="SendResult.Queue"/> on 429 or network failure.
    /// </returns>
    public async Task<SendResult> SendAsync(ErrorPayload payload)
    {
        try
        {
            var response = await _http
                .PostAsJsonAsync(LogsHiveConstants.CaptureEndpoint, payload)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                LogDebug($"[LogsHive] Sent successfully ({(int)response.StatusCode}).");
                return SendResult.Sent;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                LogDebug("[LogsHive] 401 Unauthorized — discarding entry.");
                return SendResult.Discard;
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                LogDebug("[LogsHive] 429 Too Many Requests — queuing entry.");
                return SendResult.Queue;
            }

            // Other server errors — queue for retry
            LogDebug($"[LogsHive] Unexpected status {(int)response.StatusCode} — queuing entry.");
            return SendResult.Queue;
        }
        catch (HttpRequestException ex)
        {
            LogDebug($"[LogsHive] Network error — queuing entry. ({ex.Message})");
            return SendResult.Queue;
        }
        catch (TaskCanceledException)
        {
            LogDebug("[LogsHive] Request timed out — queuing entry.");
            return SendResult.Queue;
        }
        catch (Exception ex)
        {
            LogDebug($"[LogsHive] Unexpected error — queuing entry. ({ex.Message})");
            return SendResult.Queue;
        }
    }

    [Conditional("DEBUG")]
    private static void LogDebug(string message) => Debug.WriteLine(message);

    public void Dispose() => _http.Dispose();
}

internal enum SendResult
{
    Sent,
    Discard,
    Queue
}
