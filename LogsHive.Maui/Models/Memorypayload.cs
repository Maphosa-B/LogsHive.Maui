namespace LogsHive.Maui.Models;

/// <summary>
/// The payload sent to POST /api/memory/capture.
/// Raw bytes are sent — no MB conversion. The dashboard handles display formatting.
/// Growth calculations are done on the backend by comparing snapshots with the same SessionId.
/// </summary>
internal sealed class MemoryPayload
{
    /// <summary>Human-readable app name.</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>Routes this snapshot to the correct project on the dashboard.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Generated once on app start. Groups all snapshots from this single run
    /// into one session timeline on the dashboard.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Production or Debug. Used for dashboard filtering.</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when this snapshot was taken on the device.</summary>
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Why this snapshot was sent.
    /// SustainedGrowth | AbsoluteThreshold | Manual | SessionEnd
    /// </summary>
    public string TriggerReason { get; set; } = string.Empty;

    /// <summary>Managed heap metrics — everything the GC owns and tracks.</summary>
    public SnapshotManagedMemory Managed { get; set; } = new();

    /// <summary>Native memory metrics — outside GC control.</summary>
    public SnapshotNativeMemory Native { get; set; } = new();

    /// <summary>Device context for filtering by platform, OS version, and model.</summary>
    public DeviceContext Device { get; set; } = new();

    /// <summary>
    /// Labels attached to this snapshot. Empty for automatic captures.
    /// For manual captures: e.g. ["before-gallery-load"] or ["after-gallery-load"].
    /// </summary>
    public string[] Tags { get; set; } = [];
}

/// <summary>
/// Memory the .NET runtime owns and the GC can collect.
/// Covers all C# objects: ViewModels, collections, strings, cached data.
/// </summary>
internal sealed class SnapshotManagedMemory
{
    /// <summary>
    /// Bytes currently live on the managed heap.
    /// Source: GC.GetTotalMemory(false) — false = don't force a collection first.
    /// </summary>
    public long HeapBytes { get; set; }

    /// <summary>
    /// Gen 0 collections since app start. Short-lived objects.
    /// A high rising count is normal — GC doing its job.
    /// Source: GC.CollectionCount(0)
    /// </summary>
    public int Gen0Collections { get; set; }

    /// <summary>
    /// Gen 1 collections since app start. Medium-lived objects.
    /// Source: GC.CollectionCount(1)
    /// </summary>
    public int Gen1Collections { get; set; }

    /// <summary>
    /// Gen 2 (full GC) collections since app start.
    /// Rising Gen 2 count with no heap reduction = confirmed managed leak.
    /// Source: GC.CollectionCount(2)
    /// </summary>
    public int Gen2Collections { get; set; }
}

/// <summary>
/// Memory outside the GC's visibility.
/// Covers bitmaps, platform UI buffers, SQLite pages, camera/Bluetooth buffers.
/// If this grows while HeapBytes is flat — check Dispose() calls.
/// </summary>
internal sealed class SnapshotNativeMemory
{
    /// <summary>
    /// Total RAM the OS has committed to this process.
    /// Includes managed heap + native allocations + runtime overhead.
    /// Source: Environment.WorkingSet
    /// </summary>
    public long WorkingSetBytes { get; set; }
}

/// <summary>
/// Device context sent with every snapshot for dashboard filtering.
/// </summary>
internal sealed class DeviceContext
{
    /// <summary>Android | iOS | Windows | macOS</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>e.g. "14" for Android 14, "17.2" for iOS 17.2</summary>
    public string OsVersion { get; set; } = string.Empty;

    /// <summary>e.g. "Pixel 7", "Samsung Galaxy A14"</summary>
    public string DeviceModel { get; set; } = string.Empty;

    /// <summary>App version that produced this snapshot. Used to correlate leaks with releases.</summary>
    public string AppVersion { get; set; } = string.Empty;
}