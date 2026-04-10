namespace LogsHive.Maui.Services;

/// <summary>
/// Captures a heap snapshot on creation (before) and again on disposal (after).
/// Use via LogsHiveClient.MeasureScope() with await using to bracket an operation:
/// <code>
/// await using var scope = LogsHiveClient.MeasureScope("gallery-load");
/// await LoadGalleryAsync();
/// // scope disposes here — after snapshot fires automatically
/// </code>
/// The dashboard pairs the before/after snapshots by tag prefix and shows the diff.
/// </summary>
internal sealed class MemoryMeasurementScopeService : IAsyncDisposable
{
    private readonly LogsHiveService _service;
    private readonly string _tag;

    public MemoryMeasurementScopeService(LogsHiveService service, string tag)
    {
        _service = service;
        _tag = tag;

        _service.LogLocally($"[LogsHive] MeasureScope started — capturing before-{_tag} snapshot.");

        // fire before snapshot immediately on construction
        _ = _service.CaptureMemoryAsync("Manual", tags: [$"before-{_tag}"]);
    }

    public async ValueTask DisposeAsync()
    {
        _service.LogLocally($"[LogsHive] MeasureScope ending — capturing after-{_tag} snapshot.");

        // fire after snapshot when scope exits
        await _service.CaptureMemoryAsync("Manual", tags: [$"after-{_tag}"])
            .ConfigureAwait(false);
    }
}

/// <summary>
/// No-op scope returned when the SDK is not initialized.
/// Keeps MeasureScope() safe to call regardless of initialization state.
/// </summary>
internal sealed class NullScope : IAsyncDisposable
{
    public static readonly NullScope Instance = new();
    private NullScope() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}