namespace LogsHive.Maui.Models;

/// <summary>
/// Represents a single entry persisted to logshive_queue.json
/// when the device is offline or receives a 429 response.
/// </summary>
public sealed class QueuedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ErrorPayload Payload { get; set; } = new();
    public DateTimeOffset QueuedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of send attempts made for this entry.
    /// </summary>
    public int Attempts { get; set; } = 0;
}
