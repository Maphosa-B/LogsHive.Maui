using LogsHive.Maui.Models;

namespace LogsHive.Maui.Services;

/// <summary>
/// Runs a background timer that periodically samples managed heap and working set.
/// Evaluates each sample against leak conditions and fires CaptureMemoryAsync
/// when a problem is detected.
///
/// Leak conditions checked in order on every sample:
///   1. SustainedGrowth  — N consecutive samples each higher than the last
///
/// A cooldown prevents spamming the API with repeated alerts for the same leak.
/// </summary>
internal sealed class MemoryMonitorService
{
    private readonly LogsHiveOptions _options;
    private readonly LogsHiveService _service;

    // consecutive growth tracking
    private long _previousHeapBytes;
    private int _consecutiveGrowthCount;

    // session baseline — set on first sample
    private long _baselineHeapBytes;
    private bool _baselineSet;

    // cooldown — don't fire again too soon after an alert
    private DateTimeOffset _lastAlertAt = DateTimeOffset.MinValue;
    private const int CooldownSeconds = 120;

    // SustainedGrowth fires after this many consecutive growing samples
    // will be driven by dashboard config in a future release
    private const int ConsecutiveGrowthThreshold = 3;

    public MemoryMonitorService(LogsHiveOptions options, LogsHiveService service)
    {
        _options = options;
        _service = service;
    }

    public async Task StartAsync()
    {
        _service.LogLocally($"[LogsHive] Memory monitor started. " +
                            $"Interval: {_options.MemoryMonitoringIntervalSeconds}s | " +
                            $"SustainedGrowth threshold: {ConsecutiveGrowthThreshold} samples | " +
                            $"Cooldown: {CooldownSeconds}s");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.MemoryMonitoringIntervalSeconds));

        while (await timer.WaitForNextTickAsync().ConfigureAwait(false))
        {
            await TickAsync().ConfigureAwait(false);
        }
    }

    private async Task TickAsync()
    {
        var heapBytes = GC.GetTotalMemory(false);
        var workingSet = Environment.WorkingSet;
        var heapMb = heapBytes / 1024 / 1024;
        var workingSetMb = workingSet / 1024 / 1024;

        // first sample sets the baseline
        if (!_baselineSet)
        {
            _baselineHeapBytes = heapBytes;
            _previousHeapBytes = heapBytes;
            _baselineSet = true;

            _service.LogLocally($"[LogsHive] Memory baseline set. " +
                                $"Heap: {heapMb} MB | WorkingSet: {workingSetMb} MB");
            return;
        }

        var growthFromBaseline = (heapBytes - _baselineHeapBytes) / 1024 / 1024;
        var growthFromPrevious = (heapBytes - _previousHeapBytes) / 1024 / 1024;

        _service.LogLocally($"[LogsHive] Memory tick. " +
                            $"Heap: {heapMb} MB | " +
                            $"WorkingSet: {workingSetMb} MB | " +
                            $"+{growthFromBaseline} MB from baseline | " +
                            $"{(heapBytes > _previousHeapBytes ? $"+{growthFromPrevious} MB" : "stable/dropping")} since last sample | " +
                            $"Consecutive growth: {_consecutiveGrowthCount}/{ConsecutiveGrowthThreshold}");

        var reason = Evaluate(heapBytes);

        if (reason is not null)
        {
            _service.LogLocally($"[LogsHive] Leak condition met — firing snapshot. " +
                                $"Reason: {reason} | " +
                                $"Heap: {heapMb} MB | " +
                                $"Growth from baseline: +{growthFromBaseline} MB");

            await _service.CaptureMemoryAsync(reason, tags: [])
                .ConfigureAwait(false);

            _lastAlertAt = DateTimeOffset.UtcNow;
            _consecutiveGrowthCount = 0;
        }

        _previousHeapBytes = heapBytes;
    }

    /// <summary>
    /// Evaluates the current heap sample against leak conditions.
    /// Returns a trigger reason string if a condition is met, null if healthy.
    /// </summary>
    private string? Evaluate(long currentHeapBytes)
    {
        // respect cooldown — avoid spamming the API for the same ongoing leak
        if (IsInCooldown())
        {
            var remaining = CooldownSeconds - (DateTimeOffset.UtcNow - _lastAlertAt).TotalSeconds;
            _service.LogLocally($"[LogsHive] Cooldown active — {remaining:F0}s remaining before next alert.");
            return null;
        }

        // track consecutive growth — reset on any downward movement
        if (currentHeapBytes > _previousHeapBytes)
            _consecutiveGrowthCount++;
        else
            _consecutiveGrowthCount = 0;

        // SustainedGrowth — heap has grown on every sample for N intervals
        if (_consecutiveGrowthCount >= ConsecutiveGrowthThreshold)
            return "SustainedGrowth";

        return null;
    }

    private bool IsInCooldown()
        => (DateTimeOffset.UtcNow - _lastAlertAt).TotalSeconds < CooldownSeconds;
}