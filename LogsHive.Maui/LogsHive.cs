using LogsHive.Maui.Extensions;
using LogsHive.Maui.Services;

namespace LogsHive.Maui;

/// <summary>
/// Static facade for the LogsHive SDK.
/// <para>
/// <b>Setup</b> — call once in MauiProgram.cs or App.xaml.cs:
/// <code>
/// LogsHiveClient.Configure()
///     .UseSaaS()
///     .WithApiKey("your-key")
///     .WithAppName("MyApp")
///     .ForProduction()
///     .Initialize();
/// </code>
/// — or via the MauiAppBuilder extension:
/// <code>
/// builder.UseLogsHive()
///     .UseSaaS()
///     ...
///     .Initialize();
/// </code>
/// </para>
/// <para>
/// <b>Usage</b>:
/// <code>
/// LogsHiveClient.Log("Something notable happened.");
/// LogsHiveClient.Capture(exception);
/// await LogsHiveClient.CaptureAsync(exception);
/// </code>
/// </para>
/// <para>
/// <b>Flush on resume</b> — call in App.OnStart / OnResume:
/// <code>
/// await LogsHiveClient.FlushAsync();
/// </code>
/// </para>
/// </summary>
public static class LogsHiveClient
{
    private static LogsHiveService? _service;

    // ── Configuration ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new <see cref="LogsHiveBuilder"/> for fluent configuration.
    /// </summary>
    public static LogsHiveBuilder Configure() => new LogsHiveBuilder();

    /// <summary>
    /// Called by <see cref="LogsHiveBuilder.Initialize"/> after building options.
    /// Not intended to be called directly.
    /// </summary>
    internal static void Initialize(LogsHiveService service)
    {
        _service?.Dispose();
        _service = service;
    }

    // ── Capture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a free-form log message to LogsHive.
    /// Fire-and-forget; exceptions are swallowed internally.
    /// </summary>
    public static void Log(string message)
    {
        if (_service is null) return;
        _ = _service.LogAsync(message);
    }

    /// <summary>
    /// Captures an exception synchronously (fire-and-forget).
    /// Safe to call from any thread.
    /// </summary>
    public static void Capture(Exception exception)
    {
        if (_service is null) return;
        _ = _service.CaptureAsync(exception);
    }

    /// <summary>
    /// Captures an exception asynchronously.
    /// Awaitable — useful when you need to ensure delivery before continuing.
    /// </summary>
    public static async Task CaptureAsync(Exception exception)
    {
        if (_service is null) return;
        await _service.CaptureAsync(exception).ConfigureAwait(false);
    }

    // ── Queue management ──────────────────────────────────────────────────────

    /// <summary>
    /// Flushes the offline queue. Call this in App.OnStart() and App.OnResume().\
    /// </summary>
    public static async Task FlushAsync()
    {
        if (_service is null) return;
        await _service.FlushQueueAsync().ConfigureAwait(false);
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the SDK has been initialized via <see cref="Configure"/>.
    /// </summary>
    public static bool IsInitialized => _service is not null;
}