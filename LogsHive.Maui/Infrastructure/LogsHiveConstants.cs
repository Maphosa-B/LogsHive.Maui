namespace LogsHive.Maui.Infrastructure;

internal static class LogsHiveConstants
{
    public const string SaaSBaseUrl = "https://api.logshive.io";
    public const string CaptureEndpoint = "/api/errors/capture";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string QueueFileName = "logshive_queue.json";

    /// <summary>Maximum queued entries kept on disk to avoid unbounded growth.</summary>
    public const int MaxQueueSize = 500;
}
