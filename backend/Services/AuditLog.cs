using System.Collections.Concurrent;

namespace TradingApp.Services;

public static class AuditLog
{
    private static readonly ConcurrentQueue<(DateTime Timestamp, string Snapshot)> _log = new();

    /// <summary>
    /// Adds a new audit log entry with the current UTC timestamp,
    /// and ensures only the 50 most recent entries are retained.
    /// </summary>
    /// <param name="snapshot">The snapshot string to log.</param>
    public static void Add(string snapshot)
    {
        _log.Enqueue((DateTime.UtcNow, snapshot));
        while (_log.Count > 50 && _log.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Retrieves all current audit log entries as a read-only collection.
    /// </summary>
    /// <returns>A read-only collection of timestamped snapshots.</returns>
    public static IReadOnlyCollection<(DateTime Timestamp, string Snapshot)> GetAll() => _log.ToArray();
}
