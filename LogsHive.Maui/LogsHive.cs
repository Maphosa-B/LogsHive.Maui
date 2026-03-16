using LogsHive.Maui.Services;

namespace LogsHive.Maui;

/// <summary>
/// Static facade for the LogsHive SDK.
/// <para>
/// <b>Setup</b> — call once in MauiProgram.cs:
/// <code>
/// builder.UseLogsHive(op =>
/// {
///     op.Mode        = LogsHiveMode.SaaS;
///     op.ApiKey      = "lh_your_api_key_here";
///     op.ProjectId   = "your_project_id_here";
///     op.AppName     = "MyApp";
///     op.Environment = LogsHiveEnvironmentType.Production;
///     op.Tags        = new() { ["environment"] = "production", ["tenant"] = "acme" };
/// });
/// </code>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// LogsHiveClient.Log("Something notable happened.");
/// LogsHiveClient.Capture(exception);
/// await LogsHiveClient.CaptureAsync(exception);
///
/// // With per-capture tags:
/// LogsHiveClient.Log("User checked out", tags: new() { ["screen"] = "Checkout" });
/// LogsHiveClient.Capture(ex, tags: new() { ["userId"] = "u_123" });
/// </code>
/// </para>
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

    // ── Capture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a free-form log message. Fire-and-forget.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="tags">Optional key-value tags merged with global tags.</param>
    public static void Log(string message, Dictionary<string, string>? tags = null)
    {
        if (_service is null) return;
        _ = _service.LogAsync(message, tags);
    }

    /// <summary>
    /// Captures an exception synchronously (fire-and-forget).
    /// Safe to call from any thread.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="tags">Optional key-value tags merged with global tags.</param>
    public static void Capture(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (_service is null) return;
        _ = _service.CaptureAsync(exception, tags);
    }

    /// <summary>
    /// Captures an exception asynchronously.
    /// Awaitable — useful when you need to ensure delivery before continuing.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="tags">Optional key-value tags merged with global tags.</param>
    public static async Task CaptureAsync(Exception exception, Dictionary<string, string>? tags = null)
    {
        if (_service is null) return;
        await _service.CaptureAsync(exception, tags).ConfigureAwait(false);
    }

    // ── Queue management ──────────────────────────────────────────────────────

    /// <summary>
    /// Flushes the offline queue. Call this in App.OnStart() and App.OnResume().
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