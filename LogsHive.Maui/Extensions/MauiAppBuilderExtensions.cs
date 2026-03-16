using Microsoft.Extensions.DependencyInjection;

namespace LogsHive.Maui.Extensions;

/// <summary>
/// Extension methods for wiring LogsHive into a MAUI app's DI container.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers LogsHive services and returns a <see cref="LogsHiveBuilder"/>
    /// for fluent configuration.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseLogsHive()
    ///     .UseSaaS()
    ///     .WithApiKey("your-key")
    ///     .WithAppName("MyApp")
    ///     .ForProduction()
    ///     .Initialize();
    /// </code>
    /// </example>
    public static LogsHiveBuilder UseLogsHive(this MauiAppBuilder builder)
    {
        return new LogsHiveBuilder();
    }
}
