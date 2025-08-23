using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Collections.Concurrent;
using System.Text.Json;

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

    // Task: send messages from worker → frontend
    var sendTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var msg in channel.ReadAllAsync(cts.Token))
            {
                var buffer = Encoding.UTF8.GetBytes(msg);
                await clientSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
            }
        }
        catch
        {
            Console.WriteLine("❌ Send loop ended");
        }
    });

    // Task: receive messages from frontend → server
    var receiveTask = Task.Run(async () =>
    {
        var buffer = new byte[1024];
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
            }
        }
        catch
        {
            Console.WriteLine("❌ Receive loop ended");
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

        var buffer = new byte[8192];

        Console.WriteLine("✅ Exchange worker started");

        try
        {
            while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("❌ Exchange closed");
                    break;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var transformed = Transform(json);

                OrderBookBroadcaster.Publish(transformed);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⚠️ Exchange worker stopped");
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
        return raw;
    }
}
