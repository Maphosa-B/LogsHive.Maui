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
/// Configuration options built via the fluent builder.
/// </summary>
public sealed class LogsHiveOptions
{
    internal LogsHiveMode Mode { get; set; } = LogsHiveMode.SaaS;

    /// <summary>
    /// API key sent as X-Api-Key header. Required for SaaS; optional for SelfHosted.
    /// </summary>
    internal string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the LogsHive API.
    /// SaaS default: https://api.logshive.io
    /// SelfHosted: supplied by the caller.
    /// </summary>
    internal string BaseUrl { get; set; } = "https://api.logshive.io";

    /// <summary>
    /// Human-readable name for this application, included in every payload.
    /// </summary>
    internal string AppName { get; set; } = "UnknownApp";

    /// <summary>
    /// When true the SDK is active. When false all calls are no-ops.
    /// Default: false (debug mode — opt-in to production).
    /// </summary>
    internal bool IsProduction { get; set; } = false;

    /// <summary>
    /// When true, extra diagnostic output is written to Debug.
    /// </summary>
    internal bool DebugMode { get; set; } = false;
}
