namespace LogsHive.Maui.Infrastructure;

internal static class LogsHiveConstants
{
   // public const string SaaSBaseUrl = "https://localhost:7219";
    public const string SaaSBaseUrl = "https://logs-hive-api.conversion-hive.com";
    public const string CaptureEndpoint = "/api/errors/capture";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string QueueFileName = "logshive_queue.json";

    /// <summary>Maximum queued entries kept on disk to avoid unbounded growth.</summary>
    public const int MaxQueueSize = 500;
}
