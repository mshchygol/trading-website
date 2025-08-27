using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace TradingApp.Services;

public class OrderBookBroadcaster
{
    private readonly ILogger<OrderBookBroadcaster> _logger;
    private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();

    public OrderBookBroadcaster(ILogger<OrderBookBroadcaster> logger)
    {
        _logger = logger;
    }

    public ChannelReader<string> Subscribe(Guid id)
    {
        var channel = Channel.CreateUnbounded<string>();
        _subscribers[id] = channel;
        _logger.LogInformation("[Broadcaster] Subscriber {Id} added.", id);
        return channel.Reader;
    }

    public void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.Complete();
            _logger.LogInformation("[Broadcaster] Subscriber {Id} removed.", id);
        }
    }

    public void Publish(string msg)
    {
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(msg);

        AuditLog.Add(msg);
    }
}
