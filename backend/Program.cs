using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Threading.Channels;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseWebSockets();
app.UseCors();

app.MapGet("/", () => "Hello World!");

// Shared worker instance
var exchangeWorker = new ExchangeIngestWorker();

app.MapGet("/auditlog", () =>
    Results.Json(
        AuditLog.GetAll().Select(x => new { timestamp = x.Timestamp, snapshot = x.Snapshot }),
        new JsonSerializerOptions { WriteIndented = true }
    )
);

var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

// WebSocket endpoint for frontend
app.Map("/ws/orderbook", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var clientSocket = await context.WebSockets.AcceptWebSocketAsync();
    var cts = new CancellationTokenSource();
    var clientId = Guid.NewGuid();
    Console.WriteLine($"[WS] New connection established: {context.Connection.Id} ({clientId})");

    if (!exchangeWorker.IsRunning)
    {
        Console.WriteLine("[Worker] Starting ExchangeIngestWorker...");
        _ = exchangeWorker.StartAsync();
    }

    var channel = OrderBookBroadcaster.Subscribe(clientId);
    decimal? requestedBtcAmount = null;

    // worker → frontend
    var sendTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var raw in channel.ReadAllAsync(cts.Token))
            {
                BitstampEnvelope? env;
                try { env = JsonSerializer.Deserialize<BitstampEnvelope>(raw, jsonOpts); }
                catch
                {
                    Console.WriteLine("[WS] Failed to deserialize BitstampEnvelope");
                    continue;
                }

                if (env?.Event?.Equals("data", StringComparison.OrdinalIgnoreCase) != true || env.Data == null)
                    continue;

                object payload = requestedBtcAmount is decimal buy && buy > 0
                    ? new
                    {
                        bids = env.Data.bids,
                        asks = env.Data.asks,
                        quote = new
                        {
                            btcAmount = buy,
                            eurCost = QuoteCalculator.CalculateQuote(env.Data, buy),
                            success = QuoteCalculator.CalculateQuote(env.Data, buy) >= 0
                        }
                    }
                    : env.Data;

                var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                await clientSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
    });

    // frontend → server
    var receiveTask = Task.Run(async () =>
    {
        var buffer = new byte[2048];
        try
        {
            while (clientSocket.State == WebSocketState.Open)
            {
                var result = await clientSocket.ReceiveAsync(buffer, cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (msg.Equals("close", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[WS] Client {context.Connection.Id} requested close.");
                    await exchangeWorker.StopAsync();
                    break;
                }

                try
                {
                    using var doc = JsonDocument.Parse(msg);
                    if (doc.RootElement.TryGetProperty("buyAmount", out var amt))
                    {
                        decimal? parsed = amt.ValueKind switch
                        {
                            JsonValueKind.String when decimal.TryParse(amt.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1) => d1,
                            JsonValueKind.Number when amt.TryGetDecimal(out var d2) => d2,
                            _ => null
                        };

                        if (parsed is decimal p)
                        {
                            requestedBtcAmount = p;
                            Console.WriteLine($"[WS] Client {context.Connection.Id} provided buyAmount: {p} BTC");
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("[WS] Failed to parse client message.");
                }
            }
        }
        finally
        {
            cts.Cancel();
            OrderBookBroadcaster.Unsubscribe(clientId); // ✅ unsubscribe here
            Console.WriteLine($"[WS] Connection closed: {context.Connection.Id} ({clientId})");
            await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
        }
    });

    await Task.WhenAny(sendTask, receiveTask);
});

app.Run();

// ----------------- Broadcaster -----------------
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
        foreach (var kvp in _subscribers.Values)
        {
            kvp.Writer.TryWrite(msg);
        }
        AuditLog.Add(msg);
    }
}

// ----------------- Audit Log -----------------
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

// ----------------- Exchange Worker -----------------
public class ExchangeIngestWorker
{
    private readonly Uri _uri = new("wss://ws.bitstamp.net");
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _ws?.State == WebSocketState.Open;

    public async Task StartAsync()
    {
        if (IsRunning) return;

        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        await _ws.ConnectAsync(_uri, _cts.Token);
        Console.WriteLine("[Worker] Connected to Bitstamp WebSocket.");

        var subscribe = """
        { "event": "bts:subscribe", "data": { "channel": "order_book_btceur" } }
        """;
        await _ws.SendAsync(Encoding.UTF8.GetBytes(subscribe), WebSocketMessageType.Text, true, _cts.Token);
        Console.WriteLine("[Worker] Subscribed to order_book_btceur.");

        var buffer = new byte[8192];
        try
        {
            while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(buffer, _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var rawMsg = Encoding.UTF8.GetString(ms.ToArray());
                OrderBookBroadcaster.Publish(rawMsg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Worker] Error: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        Console.WriteLine("[Worker] Stopping...");
        try
        {
            _cts?.Cancel();
            if (_ws?.State == WebSocketState.Open)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopped by server", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Worker] Stop error: {ex.Message}");
        }
        finally
        {
            _ws?.Dispose();
            _ws = null;
            _cts = null;
            Console.WriteLine("[Worker] Stopped.");
        }
    }
}

// ----------------- Models -----------------
public class BitstampEnvelope
{
    public string? Event { get; set; }
    public BitstampOrderBookData? Data { get; set; }
}
public class BitstampOrderBookData
{
    public List<List<string>> bids { get; set; } = new();
    public List<List<string>> asks { get; set; } = new();
}

// ----------------- Quote Calculator -----------------
public static class QuoteCalculator
{
    public static decimal CalculateQuote(BitstampOrderBookData book, decimal btcAmount)
    {
        if (book.asks.Count == 0) return -1m;

        decimal remaining = btcAmount, cost = 0;
        foreach (var lvl in book.asks)
        {
            if (lvl.Count < 2 ||
                !decimal.TryParse(lvl[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ||
                !decimal.TryParse(lvl[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var size)) continue;

            var take = Math.Min(remaining, size);
            cost += take * price;
            remaining -= take;
            if (remaining <= 0) break;
        }

        if (remaining > 0)
        {
            return -1m;
        }

        return cost;
    }
}
