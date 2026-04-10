using LogsHive.Maui.Services;

namespace LogsHive.Maui;

/// <summary>
/// Static facade for the LogsHive SDK.
///
/// Setup — call once in MauiProgram.cs:
/// <code>
/// builder.UseLogsHive(op =>
/// {
///     op.Mode                      = LogsHiveMode.SaaS;
///     op.ApiKey                    = "lh_your_api_key_here";
///     op.ProjectId                 = "your_project_id_here";
///     op.AppName                   = "MyApp";
///     op.SendToServer              = true;
///     op.EnableLocalConsoleLogging = true;
///
///     // optional memory monitoring
///     op.EnableMemoryMonitoring          = true;
///     op.MemoryMonitoringIntervalSeconds = 30;
/// });
/// </code>
///
/// Switch by build:
/// <code>
/// #if DEBUG
///     op.SendToServer              = false;
///     op.EnableLocalConsoleLogging = true;
/// #else
///     op.SendToServer              = true;
///     op.EnableMemoryMonitoring    = true;
/// #endif
/// </code>
///
/// Error capture:
/// <code>
/// LogsHiveClient.Log("Something notable happened.");
/// LogsHiveClient.Capture(exception);
/// await LogsHiveClient.CaptureAsync(exception);
/// </code>
///
/// Memory capture — manual snapshot:
/// <code>
/// await LogsHiveClient.CaptureHeapAsync("after-gallery-load");
/// </code>
///
/// Memory capture — scoped measurement:
/// <code>
/// await using var scope = LogsHiveClient.MeasureScope("gallery-load");
/// await LoadGalleryAsync();
/// // before + after snapshots fired automatically
/// </code>
/// </summary>
public static class LogsHiveClient
{
    private static LogsHiveService? _service;

    /// <summary>
    /// Called internally by UseLogsHive() after options are validated.
    /// Not intended to be called directly.
    /// </summary>
    internal static void Initialize(LogsHiveService service)
    {
        _service?.Dispose();
        _service = service;
    }

    // ── Error capture ─────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a free-form log message. Fire-and-forget.
    /// Only sends when SendToServer is true.
    /// </summary>
    public static void Log(string message, Dictionary<string, string>? tags = null)
    {
        if (_service is null) return;
        _ = _service.LogAsync(message, tags);
    }

    /// <summary>
    /// Captures an exception synchronously (fire-and-forget).
    /// Safe to call from any thread. Only sends when SendToServer is true.
    /// </summary>
    public static void Capture(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (_service is null) return;
        _ = _service.CaptureAsync(exception, tags);
    }

    /// <summary>
    /// Captures an exception asynchronously.
    /// Awaitable — use when you need to ensure delivery before continuing.
    /// Only sends when SendToServer is true.
    /// </summary>
    public static async Task CaptureAsync(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (_service is null) return;
        await _service.CaptureAsync(exception, tags).ConfigureAwait(false);
    }

    // ── Memory capture ────────────────────────────────────────────────────────

    /// <summary>
    /// Manually captures a heap snapshot at this exact moment.
    /// Use to pinpoint which operation is causing memory growth.
    /// Only sends when SendToServer is true.
    /// </summary>
    /// <param name="tags">
    /// Labels for this snapshot e.g. "after-gallery-load".
    /// Visible in the dashboard timeline.
    /// </param>
    public static async Task CaptureHeapAsync(params string[] tags)
    {
        if (_service is null) return;
        await _service.CaptureMemoryAsync("Manual", tags).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a scope that captures heap on creation and again on disposal.
    /// The dashboard pairs before/after snapshots and shows the diff.
    /// Only sends when SendToServer is true.
    /// </summary>
    /// <param name="tag">
    /// Label for this measurement e.g. "gallery-load".
    /// SDK prefixes automatically: "before-gallery-load" / "after-gallery-load".
    /// </param>
    public static IAsyncDisposable MeasureScope(string tag)
    {
        if (_service is null) return NullScope.Instance;
        return new MemoryMeasurementScopeService(_service, tag);
    }

    // ── Queue management ──────────────────────────────────────────────────────

    /// <summary>
    /// Flushes the offline queue. Call in App.OnStart() and App.OnResume().
    /// </summary>
    public static async Task FlushAsync()
    {
        if (_service is null) return;
        await _service.FlushQueueAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the number of events currently sitting in the offline queue.
    /// </summary>
    public static async Task<int> GetQueueCountAsync()
    {
        if (_service is null) return 0;
        return await _service.GetQueueCountAsync().ConfigureAwait(false);
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the SDK has been initialized via UseLogsHive().
    /// </summary>
    public static bool IsInitialized => _service is not null;
}