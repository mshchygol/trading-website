using System.Collections.Concurrent;

namespace TradingApp.Services;

public static class AuditLog
{
    private static readonly ConcurrentQueue<(DateTime Timestamp, string Snapshot)> _log = new();

    public static void Add(string snapshot)
    {
        _log.Enqueue((DateTime.UtcNow, snapshot));
        while (_log.Count > 50 && _log.TryDequeue(out _)) { }
    }

    public static IReadOnlyCollection<(DateTime Timestamp, string Snapshot)> GetAll() => _log.ToArray();
}
