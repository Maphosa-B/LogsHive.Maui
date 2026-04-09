using System.Diagnostics;
using LogsHive.Maui.Infrastructure;
using LogsHive.Maui.Models;

#if ANDROID
using LogsHive.Maui.Platforms.Android;
#elif IOS
using LogsHive.Maui.Platforms.iOS;
#elif MACCATALYST
using LogsHive.Maui.Platforms.MacCatalyst;
#elif WINDOWS
using LogsHive.Maui.Platforms.Windows;
#endif

namespace LogsHive.Maui.Services;

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
        _queue = new OfflineQueue(debugLogging: options.Environment == LogsHiveEnvironmentType.Debug);
        _deviceInfo = deviceInfo ?? CreatePlatformProvider();
    }

    // ── Platform provider factory ─────────────────────────────────────────────

    private static IDeviceInfoProvider CreatePlatformProvider()
    {
#if ANDROID
        return new AndroidDeviceInfoProvider();
#elif IOS
        return new iOSDeviceInfoProvider();
#elif MACCATALYST
        return new MacCatalystDeviceInfoProvider();
#elif WINDOWS
        return new WindowsDeviceInfoProvider();
#else
        return new DefaultDeviceInfoProvider();
#endif
    }

    // ── Public capture methods ───────────────────────────────────────────────

    public async Task LogAsync(string message, Dictionary<string, string>? tags = null)
    {
        if (!ShouldCapture()) return;

        var payload = BuildBasePayload(tags);
        payload.ExceptionType = "LogMessage";
        payload.Message = message;
        payload.LogMessage = message;

        await DispatchAsync(payload).ConfigureAwait(false);
    }

    public async Task CaptureAsync(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (!ShouldCapture()) return;

        var payload = BuildBasePayload(tags);
        payload.ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        payload.Message = exception.Message;
        payload.StackTrace = exception.StackTrace;
        payload.Source = exception.Source;

        await DispatchAsync(payload).ConfigureAwait(false);
    }

    public async Task FlushQueueAsync()
    {
        if (!ShouldCapture()) return;

        await _queue.FlushAsync(async payload =>
        {
            var result = await _apiClient.SendAsync(payload).ConfigureAwait(false);
            return result is SendResult.Sent or SendResult.Discard;
        }).ConfigureAwait(false);
    }

    public Task<int> GetQueueCountAsync() => _queue.GetCountAsync();

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

    private ErrorPayload BuildBasePayload(Dictionary<string, string>? perCaptureTags = null)
    {
        // Start with global tags, then overlay per-capture tags (per-capture wins on conflict)
        var mergedTags = new Dictionary<string, string>(_options.Tags);
        if (perCaptureTags is not null)
            foreach (var (key, value) in perCaptureTags)
                mergedTags[key] = value;

        return new()
        {
            AppName = _options.AppName,
            ProjectId = _options.ProjectId,
            Platform = _deviceInfo.Platform,
            OperatingSystem = _deviceInfo.OperatingSystem,
            AppVersion = _deviceInfo.AppVersion,
            DeviceModel = _deviceInfo.DeviceModel,
            CapturedAt = DateTimeOffset.UtcNow,
            Tags = mergedTags
        };
    }

    private bool ShouldCapture()
    {
        if (_options.Environment != LogsHiveEnvironmentType.Production)
        {
            LogDebug($"[LogsHive] Skipping capture — environment is {_options.Environment}.");
            return false;
        }
        return true;
    }

    private void LogDebug(string message)
    {
        #if DEBUG
        #if ANDROID
                Android.Util.Log.Debug("[LogsHive]", message);
        #else
                Debug.WriteLine(message);
        #endif
        #endif
    }

    public void Dispose() => _apiClient.Dispose();
}