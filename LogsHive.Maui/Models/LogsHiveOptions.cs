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
/// Controls whether the SDK is active and how it behaves.
/// </summary>
public enum LogsHiveEnvironmentType
{
    /// <summary>
    /// SDK is inactive. No events are sent or queued.
    /// All activity is written to Debug output only.
    /// Use during development and testing.
    /// </summary>
    Debug,

    /// <summary>
    /// SDK is active. Events are sent to the API.
    /// Use in release / production builds.
    /// </summary>
    Production
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
    /// Controls whether the SDK sends events.
    /// Default: Debug (no events sent).
    /// </summary>
    public LogsHiveEnvironmentType Environment { get; set; } = LogsHiveEnvironmentType.Debug;

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
    /// Example: https://logs.yourcompany.com
    /// </summary>
    public string? SelfHostedUrl { get; set; }

    /// <summary>
    /// Human-readable name for this application, included in every payload.
    /// </summary>
    public string AppName { get; set; } = "UnknownApp";

    /// <summary>
    /// Global tags attached to every event captured by this app.
    /// Useful for environment, tenant, build type, or any static context.
    /// Per-capture tags are merged on top of these — per-capture wins on conflict.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];

    /// <summary>
    /// When true, the SDK writes internal activity (requests, errors, queue
    /// operations) to the device log output. Useful for verifying integration
    /// during development regardless of <see cref="Environment"/>.
    /// Default: false.
    /// </summary>
    public bool EnableLocalLogging { get; set; } = false;
}