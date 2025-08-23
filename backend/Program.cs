using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

app.UseWebSockets();
app.UseCors();

app.MapGet("/", () => "Hello World!!!!!");

// Shared worker instance
var exchangeWorker = new ExchangeIngestWorker();

app.MapGet("/auditlog", () =>
{
    var log = AuditLog.GetAll()
        .Select(entry => new
        {
            timestamp = entry.Timestamp,
            snapshot = entry.Snapshot
        });

    return Results.Json(log, new JsonSerializerOptions { WriteIndented = true });
});

// ---- JSON options: make property name matching case-insensitive
JsonSerializerOptions JsonOpts = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

// WebSocket endpoint for frontend clients
app.Map("/ws/orderbook", async context =>
{
    Console.WriteLine("Frontend connected to /ws/orderbook");

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var clientSocket = await context.WebSockets.AcceptWebSocketAsync();
    var cts = new CancellationTokenSource();

    // Start worker if not already running
    if (!exchangeWorker.IsRunning)
    {
        _ = exchangeWorker.StartAsync();
    }

    // Subscribe this client to updates
    var channel = OrderBookBroadcaster.Subscribe();
    Console.WriteLine("Client subscribed");

    // Per-client requested BTC amount (market buy simulation)
    decimal? requestedBtcAmount = null;

    // Task: send messages from worker → frontend
    var sendTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var raw in channel.ReadAllAsync(cts.Token))
            {
                BitstampEnvelope? envelope = null;
                try
                {
                    envelope = JsonSerializer.Deserialize<BitstampEnvelope>(raw, JsonOpts);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠️ JSON parse error: " + ex.Message);
                    continue; // skip malformed frame
                }

                // We only forward real order book updates
                if (envelope?.Event is null ||
                    !envelope.Event.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                    envelope.Data is null)
                {
                    continue;
                }

                object payload;

                if (requestedBtcAmount is decimal buy && buy > 0)
                {
                    var eurCost = QuoteCalculator.CalculateQuote(envelope.Data, buy);

                    payload = new
                    {
                        bids = envelope.Data.bids,
                        asks = envelope.Data.asks,
                        quote = new
                        {
                            btcAmount = buy,
                            eurCost = eurCost,       // -1 if not enough liquidity
                            success = eurCost >= 0
                        }
                    };
                }
                else
                {
                    payload = envelope.Data; // send plain orderbook until user asks for quote
                }

                var json = JsonSerializer.Serialize(payload);
                var buffer = Encoding.UTF8.GetBytes(json);
                await clientSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Send loop error: " + ex.Message);
        }
    });

    // Task: receive messages from frontend → server
    var receiveTask = Task.Run(async () =>
    {
        var buffer = new byte[2048];
        try
        {
            while (clientSocket.State == WebSocketState.Open)
            {
                var result = await clientSocket.ReceiveAsync(buffer, cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("❌ Frontend requested close (WS close)");
                    break;
                }

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (msg.Equals("close", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("❌ Frontend requested stop (\"close\" msg)");
                    await exchangeWorker.StopAsync();
                    break;
                }

                // Accept JSON like: { "buyAmount": 0.5 }  (you can also send strings)
                try
                {
                    using var doc = JsonDocument.Parse(msg);
                    if (doc.RootElement.TryGetProperty("buyAmount", out var amt))
                    {
                        if (amt.ValueKind == JsonValueKind.String &&
                            decimal.TryParse(amt.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1))
                        {
                            requestedBtcAmount = d1;
                        }
                        else if (amt.ValueKind == JsonValueKind.Number &&
                                 amt.TryGetDecimal(out var d2))
                        {
                            requestedBtcAmount = d2;
                        }

                        Console.WriteLine($"✅ Client requested BTC quote for {requestedBtcAmount} BTC");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Unknown client message: {msg} ({ex.Message})");
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Receive loop error: " + ex.Message);
        }
        finally
        {
            cts.Cancel();
            await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
        }
    });

    await Task.WhenAny(sendTask, receiveTask);
});

app.Run();

// ----------------- Broadcaster -----------------
public static class OrderBookBroadcaster
{
    private static readonly Channel<string> _channel =
        Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });

    public static ChannelReader<string> Subscribe() => _channel.Reader;

    public static void Publish(string msg)
    {
        _channel.Writer.TryWrite(msg);
        AuditLog.Add(msg); // ✅ also log snapshot
    }
}

// ----------------- Audit Log -----------------
public static class AuditLog
{
    private static readonly ConcurrentQueue<(DateTime Timestamp, string Snapshot)> _log = new();

    public static void Add(string snapshot)
    {
        _log.Enqueue((DateTime.UtcNow, snapshot));

        // Keep only last 50
        while (_log.Count > 50 && _log.TryDequeue(out _)) { }
    }

    public static IReadOnlyCollection<(DateTime Timestamp, string Snapshot)> GetAll()
    {
        return _log.ToArray();
    }
}

// ----------------- Exchange Worker -----------------
public class ExchangeIngestWorker
{
    private readonly Uri _exchangeUri = new("wss://ws.bitstamp.net");
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _ws != null && _ws.State == WebSocketState.Open;

    public async Task StartAsync()
    {
        if (IsRunning) return; // Already running

        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        await _ws.ConnectAsync(_exchangeUri, _cts.Token);

        var subscribe = """
        {
          "event": "bts:subscribe",
          "data": { "channel": "order_book_btceur" }
        }
        """;
        await _ws.SendAsync(Encoding.UTF8.GetBytes(subscribe), WebSocketMessageType.Text, true, _cts.Token);

        Console.WriteLine("✅ Exchange worker started");

        try
        {
            // Receive loop (collect full messages in case of fragmentation)
            var buffer = new byte[8192];
            while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult? result;

                do
                {
                    result = await _ws.ReceiveAsync(buffer, _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("❌ Exchange closed");
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(ms.ToArray());

                // publish raw Bitstamp envelope; filtering is done in the endpoint
                OrderBookBroadcaster.Publish(json);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⚠️ Exchange worker stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Exchange worker error: " + ex.Message);
        }
    }

    public async Task StopAsync()
    {
        if (_ws == null) return;

        try
        {
            _cts?.Cancel();
            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopped by server", CancellationToken.None);
            }
        }
        catch { }

        _ws.Dispose();
        _ws = null;
        _cts = null;

        Console.WriteLine("✅ Exchange worker stopped and cleaned up");
    }

    private string Transform(string raw)
    {
        // Left as-is; we filter in the endpoint.
        return raw;
    }
}

// ----------------- Models (case-insensitive deserialization enabled above) -----------------
public class BitstampEnvelope
{
    public string? Event { get; set; }           // "data", "bts:heartbeat", etc.
    public BitstampOrderBookData? Data { get; set; }
}

public class BitstampOrderBookData
{
    public List<List<string>>? bids { get; set; } = new(); // [["price","amount"], ...]
    public List<List<string>>? asks { get; set; } = new();
}

// ----------------- Quote Calculator -----------------
public static class QuoteCalculator
{
    public static decimal CalculateQuote(BitstampOrderBookData book, decimal btcAmount)
    {
        if (book.asks is null || book.asks.Count == 0) return -1m;

        decimal remaining = btcAmount;
        decimal totalCost = 0m;

        foreach (var level in book.asks)
        {
            if (level.Count < 2) continue;

            // Bitstamp sends prices and sizes as strings using '.' decimal separator
            if (!decimal.TryParse(level[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var price)) continue;
            if (!decimal.TryParse(level[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var size)) continue;

            var take = Math.Min(remaining, size);
            totalCost += take * price;
            remaining -= take;

            if (remaining <= 0) break;
        }

        return remaining > 0 ? -1m : totalCost;
    }
}
