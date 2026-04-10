using System.Diagnostics;
using LogsHive.Maui.Infrastructure;
using LogsHive.Maui.Interfaces;
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
    private readonly bool _localLogging;

    // generated once per app session — groups all snapshots from this run
    private readonly string _sessionId = Guid.NewGuid().ToString("N")[..10];

    public LogsHiveService(LogsHiveOptions options, IDeviceInfoProvider? deviceInfo = null)
    {
        _options = options;
        _localLogging = options.EnableLocalConsoleLogging;
        _apiClient = new ApiClient(options);
        _queue = new OfflineQueue(localLogging: options.EnableLocalConsoleLogging);
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

    // ── Error capture ─────────────────────────────────────────────────────────

    public async Task LogAsync(string message, Dictionary<string, string>? tags = null)
    {
        if (!SendToServer()) return;

        var payload = BuildErrorPayload(tags);
        payload.ExceptionType = "LogMessage";
        payload.Message = message;
        payload.LogMessage = message;

        await DispatchErrorAsync(payload).ConfigureAwait(false);
    }

    public async Task CaptureAsync(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (!SendToServer()) return;

        var payload = BuildErrorPayload(tags);
        payload.ExceptionType = exception.GetType().FullName ?? exception.GetType().Name;
        payload.Message = exception.Message;
        payload.StackTrace = exception.StackTrace;
        payload.Source = exception.Source;

        await DispatchErrorAsync(payload).ConfigureAwait(false);
    }

    // ── Memory capture ────────────────────────────────────────────────────────

    public async Task CaptureMemoryAsync(string triggerReason, string[] tags)
    {
        if (!SendToServer())
        {
            LogLocally($"[LogsHive] Memory snapshot skipped — SendToServer is false. Reason: {triggerReason}");
            return;
        }

        var payload = BuildMemoryPayload(triggerReason, tags);

        LogLocally($"[LogsHive] Capturing memory snapshot. " +
                   $"Reason: {triggerReason} | " +
                   $"Heap: {payload.Managed.HeapBytes / 1024 / 1024} MB | " +
                   $"WorkingSet: {payload.Native.WorkingSetBytes / 1024 / 1024} MB | " +
                   $"Gen0: {payload.Managed.Gen0Collections} " +
                   $"Gen1: {payload.Managed.Gen1Collections} " +
                   $"Gen2: {payload.Managed.Gen2Collections}" +
                   (tags.Length > 0 ? $" | Tags: {string.Join(", ", tags)}" : ""));

        await DispatchMemoryAsync(payload).ConfigureAwait(false);
    }

    // ── Queue management ──────────────────────────────────────────────────────

    public async Task FlushQueueAsync()
    {
        if (!SendToServer()) return;

        await _queue.FlushAsync(async payload =>
        {
            var result = await _apiClient.SendAsync(payload).ConfigureAwait(false);
            return result is SendResult.Sent or SendResult.Discard;
        }).ConfigureAwait(false);
    }

    public Task<int> GetQueueCountAsync() => _queue.GetCountAsync();

    // ── Private dispatch ──────────────────────────────────────────────────────

    private async Task DispatchErrorAsync(ErrorPayload payload)
    {
        var result = await _apiClient.SendAsync(payload).ConfigureAwait(false);

        switch (result)
        {
            case SendResult.Sent:
                break;
            case SendResult.Discard:
                LogLocally("[LogsHive] Error entry discarded (401).");
                break;
            case SendResult.Queue:
                await _queue.EnqueueAsync(payload).ConfigureAwait(false);
                break;
        }
    }

    private async Task DispatchMemoryAsync(MemoryPayload payload)
    {
        var result = await _apiClient.SendMemoryAsync(payload).ConfigureAwait(false);

        switch (result)
        {
            case SendResult.Sent:
                LogLocally("[LogsHive] Memory snapshot sent successfully.");
                break;
            case SendResult.Discard:
                // ApiClient already logged the URL + 401 detail
                // this adds context that memory snapshots are discarded, not retried
                LogLocally("[LogsHive] Memory snapshot discarded — will not retry.");
                break;
            case SendResult.Queue:
                // ApiClient already logged the URL + status code + detail
                // memory snapshots are intentionally not queued — stale data is useless
                LogLocally("[LogsHive] Memory snapshot dropped — not queued (time-sensitive data).");
                break;
        }
    }

    // ── Payload builders ──────────────────────────────────────────────────────

    private ErrorPayload BuildErrorPayload(Dictionary<string, string>? perCaptureTags = null)
    {
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

    private MemoryPayload BuildMemoryPayload(string triggerReason, string[] tags)
    {
        return new()
        {
            AppName = _options.AppName,
            ProjectId = _options.ProjectId,
            SessionId = _sessionId,
            CapturedAt = DateTimeOffset.UtcNow,
            TriggerReason = triggerReason,

            Managed = new SnapshotManagedMemory
            {
                HeapBytes = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            },

            Native = new SnapshotNativeMemory
            {
                WorkingSetBytes = Environment.WorkingSet
            },

            Device = new DeviceContext
            {
                Platform = _deviceInfo.Platform,
                OsVersion = _deviceInfo.OperatingSystem,
                DeviceModel = _deviceInfo.DeviceModel,
                AppVersion = _deviceInfo.AppVersion
            },

            Tags = tags
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool SendToServer() => _options.SendToServer;

    internal void LogLocally(string message)
    {
        if (!_localLogging) return;

#if ANDROID
        Android.Util.Log.Debug("[LogsHive]", message);
#else
        Debug.WriteLine(message);
#endif
    }

    public void Dispose() => _apiClient.Dispose();
}