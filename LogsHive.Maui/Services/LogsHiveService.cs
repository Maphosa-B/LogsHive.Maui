using System.Diagnostics;
using LogsHive.Maui.Infrastructure;
using LogsHive.Maui.Models;

namespace LogsHive.Maui.Services;

/// <summary>
/// Core service that builds payloads, dispatches to the API,
/// and manages the offline queue.
/// </summary>
internal sealed class LogsHiveService : IDisposable
{
    private readonly LogsHiveOptions _options;
    private readonly ApiClient _apiClient;
    private readonly OfflineQueue _queue;
    private readonly IDeviceInfoProvider _deviceInfo;

    public LogsHiveService(LogsHiveOptions options, IDeviceInfoProvider? deviceInfo = null)
    {
        _options = options;
        _apiClient = new ApiClient(options);
        _queue = new OfflineQueue();
        _deviceInfo = deviceInfo ?? new DefaultDeviceInfoProvider();
    }

    // ── Public capture methods ───────────────────────────────────────────────

    /// <summary>Captures a free-form log message.</summary>
    public async Task LogAsync(string message)
    {
        if (!ShouldCapture()) return;

        var payload = BuildBasePayload();
        payload.ExceptionType = "LogMessage";
        payload.Message = message;
        payload.LogMessage = message;

        await DispatchAsync(payload).ConfigureAwait(false);
    }

    /// <summary>Captures an exception.</summary>
    public async Task CaptureAsync(Exception exception)
    {
        if (!ShouldCapture()) return;

        var payload = BuildBasePayload();
        payload.ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        payload.Message = exception.Message;
        payload.StackTrace = exception.StackTrace;
        payload.Source = exception.Source;

        await DispatchAsync(payload).ConfigureAwait(false);
    }

    /// <summary>
    /// Flushes any queued entries from disk.
    /// Should be called from the app's OnStart / OnResume lifecycle.
    /// </summary>
    public async Task FlushQueueAsync()
    {
        if (!ShouldCapture()) return;

        await _queue.FlushAsync(async payload =>
        {
            var result = await _apiClient.SendAsync(payload).ConfigureAwait(false);
            // Only treat Sent as success; Queue means try again later, Discard drops it
            return result is SendResult.Sent or SendResult.Discard;
        }).ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task DispatchAsync(ErrorPayload payload)
    {
        var result = await _apiClient.SendAsync(payload).ConfigureAwait(false);

        switch (result)
        {
            case SendResult.Sent:
                break;

            case SendResult.Discard:
                LogDebug("[LogsHive] Entry discarded (401).");
                break;

            case SendResult.Queue:
                await _queue.EnqueueAsync(payload).ConfigureAwait(false);
                break;
        }
    }

    private ErrorPayload BuildBasePayload() => new()
    {
        AppName = _options.AppName,
        Platform = _deviceInfo.Platform,
        OperatingSystem = _deviceInfo.OperatingSystem,
        AppVersion = _deviceInfo.AppVersion,
        DeviceModel = _deviceInfo.DeviceModel,
        CapturedAt = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Returns false when in debug/non-production mode so no events are sent.
    /// </summary>
    private bool ShouldCapture()
    {
        if (!_options.IsProduction)
        {
            LogDebug("[LogsHive] Skipping capture — not in production mode.");
            return false;
        }
        return true;
    }

    [Conditional("DEBUG")]
    private static void LogDebug(string message) => Debug.WriteLine(message);

    public void Dispose() => _apiClient.Dispose();
}
