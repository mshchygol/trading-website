using System.Collections.Concurrent;
using System.Threading.Channels;

namespace YourApp.Services;

public static class OrderBookBroadcaster
{
    private static readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();

    public static ChannelReader<string> Subscribe(Guid id)
    {
        var channel = Channel.CreateUnbounded<string>();
        _subscribers[id] = channel;
        Console.WriteLine($"[Broadcaster] Subscriber {id} added.");
        return channel.Reader;
    }

    public static void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.Complete();
            Console.WriteLine($"[Broadcaster] Subscriber {id} removed.");
        }
    }

    public static void Publish(string msg)
    {
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(msg);

        AuditLog.Add(msg);
    }
}
