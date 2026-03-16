namespace LogsHive.Maui.Models;

/// <summary>
/// The payload sent to POST /api/errors/capture.
/// </summary>
internal sealed class ErrorPayload
{
    public string AppName { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LogMessage { get; set; }

    /// <summary>
    /// Arbitrary key-value pairs attached to this event.
    /// Merged from global tags (set via LogsHiveOptions.Tags)
    /// and per-capture tags (passed directly to Capture/Log calls).
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];
}