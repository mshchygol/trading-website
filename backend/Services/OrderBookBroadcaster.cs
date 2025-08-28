using System.Collections.Concurrent;
using System.Threading.Channels;

namespace TradingApp.Services;

/// <summary>
/// Manages subscriptions to order book updates, allowing multiple clients
/// to receive published messages in real time.
/// </summary>
public class OrderBookBroadcaster
{
    private readonly ILogger<OrderBookBroadcaster> _logger;
    private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderBookBroadcaster"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    public OrderBookBroadcaster(ILogger<OrderBookBroadcaster> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribes a client to the broadcaster and returns a channel reader
    /// for receiving published order book updates.
    /// </summary>
    /// <param name="id">The unique identifier of the subscriber.</param>
    /// <returns>A <see cref="ChannelReader{T}"/> that provides the stream of messages.</returns>
    public ChannelReader<string> Subscribe(Guid id)
    {
        var channel = Channel.CreateUnbounded<string>();
        _subscribers[id] = channel;
        _logger.LogInformation("[Broadcaster] Subscriber {Id} added.", id);
        return channel.Reader;
    }

    /// <summary>
    /// Unsubscribes a client and completes its message channel.
    /// </summary>
    /// <param name="id">The unique identifier of the subscriber.</param>
    public void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
        {
            channel.Writer.Complete();
            _logger.LogInformation("[Broadcaster] Subscriber {Id} removed.", id);
        }
    }

    /// <summary>
    /// Publishes a message to all active subscribers and adds it to the audit log.
    /// </summary>
    /// <param name="msg">The message to broadcast.</param>
    public void Publish(string msg)
    {
        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(msg);

        AuditLog.Add(msg);
    }
}
