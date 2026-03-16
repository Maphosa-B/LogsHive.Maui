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
    ///     op.Mode = LogsHiveMode.SaaS;
    ///     op.ApiKey = "lh_your_api_key_here";
    ///     op.ProjectId = "your_project_id_here";
    ///     op.AppName = "MyApp";
    ///     op.Environment = LogsHiveEnvironmentType.Production;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseLogsHive(this MauiAppBuilder builder, Action<LogsHiveOptions> configure)
    {
        var options = new LogsHiveOptions();
        configure(options);

        Validate(options);

        // Resolve base URL
        if (options.Mode == LogsHiveMode.SelfHosted)
        {
            if (string.IsNullOrWhiteSpace(options.SelfHostedUrl))
                throw new InvalidOperationException(
                    "SelfHostedUrl must be set when Mode is LogsHiveMode.SelfHosted.");

            options.SelfHostedUrl = options.SelfHostedUrl!.TrimEnd('/');
        }

        var service = new LogsHiveService(options);
        LogsHiveClient.Initialize(service);

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
    }
}