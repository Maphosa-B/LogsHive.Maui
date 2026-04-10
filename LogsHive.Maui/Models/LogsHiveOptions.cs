namespace LogsHive.Maui.Models;

/// <summary>
/// Deployment mode for the LogsHive SDK.
/// </summary>
public enum LogsHiveMode
{
    SaaS,
    SelfHosted
}

/// <summary>
/// Configuration options passed to the LogsHive SDK via the options delegate.
/// </summary>
public sealed class LogsHiveOptions
{
    /// <summary>
    /// Deployment mode. Use <see cref="LogsHiveMode.SaaS"/> for the hosted service
    /// or <see cref="LogsHiveMode.SelfHosted"/> for your own infrastructure.
    /// Default: SaaS.
    /// </summary>
    public LogsHiveMode Mode { get; set; } = LogsHiveMode.SaaS;

    /// <summary>
    /// Controls whether the SDK sends captured events to the server.
    /// Set to true in production builds, false during development.
    /// When false, all SDK activity is written to the local console only —
    /// nothing leaves the device.
    /// Default: false.
    /// </summary>
    public bool SendToServer { get; set; } = false;

    /// <summary>
    /// API key sent as the X-Api-Key header.
    /// Required for SaaS; optional for SelfHosted.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Unique project identifier. Routes events to the correct project
    /// on both SaaS and self-hosted instances.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for a self-hosted LogsHive API instance.
    /// Only required when <see cref="Mode"/> is <see cref="LogsHiveMode.SelfHosted"/>.
    /// Set this to your base path — the SDK appends /errors/capture and
    /// /memory/capture automatically.
    /// Example: https://logs.yourcompany.com/api
    /// </summary>
    public string? SelfHostedUrl { get; set; }

    /// <summary>
    /// Human-readable name for this application, included in every payload.
    /// </summary>
    public string AppName { get; set; } = "UnknownApp";

    /// <summary>
    /// Global tags attached to every event captured by this app.
    /// Per-capture tags are merged on top of these — per-capture wins on conflict.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];

    /// <summary>
    /// When true, the SDK writes all internal activity to the device log —
    /// sends, failures, queue operations, memory samples, and alerts.
    /// Works independently of SendToServer, so you can watch SDK activity
    /// in the Output window even in a live production build.
    /// Default: false.
    /// </summary>
    public bool EnableLocalConsoleLogging { get; set; } = false;

    /// <summary>
    /// When true, starts a background timer that automatically samples
    /// managed heap and working set, and sends a snapshot when a leak
    /// is detected. Thresholds are configured in the LogsHive dashboard.
    /// Default: false.
    /// </summary>
    public bool EnableMemoryMonitoring { get; set; } = false;

    /// <summary>
    /// How often the memory monitor samples heap and working set in seconds.
    /// Performance decision owned by the developer — affects battery and
    /// background thread usage.
    /// Only validated when EnableMemoryMonitoring is true.
    /// Default: 30. Minimum: 10.
    /// </summary>
    public int MemoryMonitoringIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Resolved at startup by MauiAppBuilderExtensions — do not set directly.
    /// Either the SaaS base URL or the trimmed SelfHostedUrl.
    /// ApiClient appends endpoint suffixes to this.
    /// </summary>
    internal string ResolvedBaseUrl { get; set; } = string.Empty;
}