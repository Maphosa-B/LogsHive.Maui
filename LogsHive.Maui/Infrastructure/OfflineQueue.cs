using System.Diagnostics;
using System.Text.Json;
using LogsHive.Maui.Models;

namespace LogsHive.Maui.Infrastructure;

/// <summary>
/// Persists <see cref="QueuedEntry"/> items to logshive_queue.json in
/// <see cref="FileSystem.AppDataDirectory"/> and flushes them when online.
/// </summary>
internal sealed class OfflineQueue
{
    private readonly string _queuePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly bool _localLogging;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public OfflineQueue(bool localLogging = false)
    {
        _localLogging = localLogging;
        _queuePath = Path.Combine(FileSystem.AppDataDirectory, LogsHiveConstants.QueueFileName);
    }

    /// <summary>Adds an entry to the on-disk queue.</summary>
    public async Task EnqueueAsync(ErrorPayload payload)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var entries = await ReadEntriesAsync().ConfigureAwait(false);

            while (entries.Count >= LogsHiveConstants.MaxQueueSize)
                entries.RemoveAt(0);

            entries.Add(new QueuedEntry { Payload = payload });
            await WriteEntriesAsync(entries).ConfigureAwait(false);

            LogLocally($"[LogsHive] Queued entry. Queue size: {entries.Count}");
        }
        catch (Exception ex)
        {
            LogLocally($"[LogsHive] Failed to queue entry: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Attempts to flush all queued entries using the provided send delegate.
    /// Successfully sent entries are removed from disk.
    /// </summary>
    public async Task FlushAsync(Func<ErrorPayload, Task<bool>> sendDelegate)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var entries = await ReadEntriesAsync().ConfigureAwait(false);
            if (entries.Count == 0) return;

            LogLocally($"[LogsHive] Flushing {entries.Count} queued entries.");

            var remaining = new List<QueuedEntry>();

            foreach (var entry in entries)
            {
                entry.Attempts++;
                bool sent = await sendDelegate(entry.Payload).ConfigureAwait(false);

                if (!sent)
                    remaining.Add(entry);
            }

            await WriteEntriesAsync(remaining).ConfigureAwait(false);
            LogLocally($"[LogsHive] Flush complete. Remaining: {remaining.Count}");
        }
        catch (Exception ex)
        {
            LogLocally($"[LogsHive] Flush error: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Returns the current count of queued entries.</summary>
    public async Task<int> GetCountAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var entries = await ReadEntriesAsync().ConfigureAwait(false);
            return entries.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<List<QueuedEntry>> ReadEntriesAsync()
    {
        if (!File.Exists(_queuePath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(_queuePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<QueuedEntry>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task WriteEntriesAsync(List<QueuedEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, _jsonOptions);
        await File.WriteAllTextAsync(_queuePath, json).ConfigureAwait(false);
    }

    private void LogLocally(string message)
    {
        if (!_localLogging) return;

        #if ANDROID
                Android.Util.Log.Debug("[LogsHive]", message);
        #else
                Debug.WriteLine(message);
        #endif
    }
}