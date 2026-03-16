namespace LogsHive.Maui.Models;

/// <summary>
/// The payload sent to POST /api/errors/capture.
/// </summary>
public sealed class ErrorPayload
{
    public string AppName { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }

    /// <summary>
    /// The source file or namespace where the exception originated.
    /// Used as part of the fingerprint hash on the server.
    /// </summary>
    public string? Source { get; set; }

    public string Platform { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional free-form log message (used when LogsHive.Log() is called).
    /// </summary>
    public string? LogMessage { get; set; }
}
