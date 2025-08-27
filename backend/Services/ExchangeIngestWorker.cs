using System.Net.WebSockets;
using System.Text;

namespace TradingApp.Services;

public class ExchangeIngestWorker
{
    private readonly Uri _uri;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _ws?.State == WebSocketState.Open;
    private readonly ILogger<ExchangeIngestWorker> _logger;
    private readonly OrderBookBroadcaster _broadcaster;


    public ExchangeIngestWorker(IConfiguration config, ILogger<ExchangeIngestWorker> logger, OrderBookBroadcaster broadcaster)
    {
        _uri = new Uri(config["AppSettings:BitstampWSUrl"]!);
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public async Task StartAsync()
    {
        if (IsRunning) return;

        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        await _ws.ConnectAsync(_uri, _cts.Token);
        _logger.LogInformation("[Worker] Connected to Bitstamp WebSocket.");

        var subscribe = """
        { "event": "bts:subscribe", "data": { "channel": "order_book_btceur" } }
        """;
        await _ws.SendAsync(Encoding.UTF8.GetBytes(subscribe), WebSocketMessageType.Text, true, _cts.Token);

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
                _broadcaster.Publish(rawMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Worker] Error: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("[Worker] Stopping...");
        try
        {
            _cts?.Cancel();
            if (_ws?.State == WebSocketState.Open)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopped by server", CancellationToken.None);
        }
        finally
        {
            _ws?.Dispose();
            _ws = null;
            _cts = null;
            _logger.LogInformation("[Worker] Stopped.");
        }
    }
}
