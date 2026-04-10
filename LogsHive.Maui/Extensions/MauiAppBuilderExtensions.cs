using LogsHive.Maui.Infrastructure;
using LogsHive.Maui.Models;
using LogsHive.Maui.Services;

namespace LogsHive.Maui.Extensions;

/// <summary>
/// Extension methods for wiring LogsHive into a MAUI app.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Initializes the LogsHive SDK using an options delegate.
    /// </summary>
    /// <example>
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
    ///     // optional memory leak detection
    ///     op.EnableMemoryMonitoring          = true;
    ///     op.MemoryMonitoringIntervalSeconds = 30;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseLogsHive(this MauiAppBuilder builder, Action<LogsHiveOptions> configure)
    {
        var options = new LogsHiveOptions();
        configure(options);

        Validate(options);

        // resolve base URL once — ApiClient appends endpoint suffixes to this
        options.ResolvedBaseUrl = options.Mode == LogsHiveMode.SelfHosted
            ? options.SelfHostedUrl!.TrimEnd('/')
            : LogsHiveConstants.SaaSBaseUrl;

        var service = new LogsHiveService(options);
        LogsHiveClient.Initialize(service);

        // start background memory monitor if opted in
        // Task.Run with async lambda ensures exceptions propagate correctly
        if (options.EnableMemoryMonitoring)
        {
            var monitor = new MemoryMonitorService(options, service);
            _ = Task.Run(async () => await monitor.StartAsync());
        }

        return builder;
    }

    private static void Validate(LogsHiveOptions options)
    {
        if (options.Mode == LogsHiveMode.SaaS && string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(
                "ApiKey is required when Mode is LogsHiveMode.SaaS.");

        if (string.IsNullOrWhiteSpace(options.ProjectId))
            throw new InvalidOperationException(
                "ProjectId is required. Set op.ProjectId before calling UseLogsHive.");

        if (options.Mode == LogsHiveMode.SelfHosted && string.IsNullOrWhiteSpace(options.SelfHostedUrl))
            throw new InvalidOperationException(
                "SelfHostedUrl must be set when Mode is LogsHiveMode.SelfHosted.");

        // only validate interval when memory monitoring is actually enabled
        // avoids crashing apps that set this property while monitoring is off
        if (options.EnableMemoryMonitoring && options.MemoryMonitoringIntervalSeconds < 10)
            throw new InvalidOperationException(
                "MemoryMonitoringIntervalSeconds must be at least 10 seconds. " +
                "The default is 30 — only change this if you need a faster interval.");
    }
}