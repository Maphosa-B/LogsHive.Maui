using LogsHive.Maui.Infrastructure;
using LogsHive.Maui.Models;
using LogsHive.Maui.Services;

namespace LogsHive.Maui.Extensions;

/// <summary>
/// Fluent builder that constructs <see cref="LogsHiveOptions"/> and
/// registers the <see cref="LogsHiveService"/> with the <see cref="LogsHiveClient"/> facade.
/// </summary>
public sealed class LogsHiveBuilder
{
    private readonly LogsHiveOptions _options = new();

    internal LogsHiveBuilder() { }

    // ── Mode ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Points the SDK at the LogsHive SaaS API (https://api.logshive.io).
    /// </summary>
    public LogsHiveBuilder UseSaaS()
    {
        _options.Mode = LogsHiveMode.SaaS;
        _options.BaseUrl = LogsHiveConstants.SaaSBaseUrl;
        return this;
    }

    /// <summary>
    /// Points the SDK at a self-hosted LogsHive API instance.
    /// </summary>
    /// <param name="baseUrl">Base URL of your self-hosted instance, e.g. https://logs.mycompany.com</param>
    public LogsHiveBuilder UseSelfHosted(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("baseUrl must not be empty.", nameof(baseUrl));

        _options.Mode = LogsHiveMode.SelfHosted;
        _options.BaseUrl = baseUrl.TrimEnd('/');
        return this;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the API key sent as the X-Api-Key header.
    /// Required for SaaS; optional for SelfHosted.
    /// </summary>
    public LogsHiveBuilder WithApiKey(string apiKey)
    {
        _options.ApiKey = apiKey;
        return this;
    }

    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the app name included in every captured event.
    /// </summary>
    public LogsHiveBuilder WithAppName(string appName)
    {
        _options.AppName = appName;
        return this;
    }

    // ── Environment ──────────────────────────────────────────────────────────

    /// <summary>
    /// Activates the SDK. Events will be sent to the API.
    /// Call this in your release/production configuration.
    /// </summary>
    public LogsHiveBuilder ForProduction()
    {
        _options.IsProduction = true;
        _options.DebugMode = false;
        return this;
    }

    /// <summary>
    /// Keeps the SDK inactive (no events sent) but enables verbose debug output.
    /// This is the default state — call <see cref="ForProduction"/> to activate.
    /// </summary>
    public LogsHiveBuilder WithDebugMode()
    {
        _options.IsProduction = false;
        _options.DebugMode = true;
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finalizes configuration and initializes the <see cref="LogsHiveClient"/> facade.
    /// </summary>
    public void Initialize()
    {
        ValidateOptions();
        var service = new LogsHiveService(_options);
        LogsHiveClient.Initialize(service);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private void ValidateOptions()
    {
        if (_options.Mode == LogsHiveMode.SaaS && string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "An API key is required for SaaS mode. Call .WithApiKey(\"your-key\") before .Initialize().");
    }
}