namespace LogsHive.Maui.Infrastructure;

internal static class LogsHiveConstants
{
    // SaaS base URL — all endpoint paths are appended to this
    // public const string SaaSBaseUrl = "https://localhost:7219";
    public const string SaaSBaseUrl = "https://logs-hive-api.conversion-hive.com/api";

    // endpoint path suffixes — appended to ResolvedBaseUrl at runtime
    // self-hosted: https://your-api.com/errors/capture
    // self-hosted: https://your-api.com/memory/capture
    // SaaS:        https://logs-hive-api.conversion-hive.com/api/errors/capture
    // SaaS:        https://logs-hive-api.conversion-hive.com/api/memory/capture
    public const string ErrorsCaptureEndpoint = "errors/capture";
    public const string MemoryCaptureEndpoint = "memory/capture";  

    public const string ApiKeyHeader = "X-Api-Key";
    public const string QueueFileName = "logshive_queue.json";

    /// <summary>Maximum queued entries kept on disk to avoid unbounded growth.</summary>
    public const int MaxQueueSize = 500;
}