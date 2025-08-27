using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Globalization;
using TradingApp.Models;
using TradingApp.Services;
using System.Threading.Channels;

namespace TradingApp.Endpoints;

public interface IOrderBookWebSocketHandler
{
    Task HandleConnectionAsync(HttpContext context, WebSocket socket);
}

public class OrderBookWebSocketHandler : IOrderBookWebSocketHandler
{
    private readonly ExchangeIngestWorker _exchangeWorker;
    private readonly OrderBookBroadcaster _broadcaster;
    private readonly ILogger<OrderBookWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OrderBookWebSocketHandler(
        ExchangeIngestWorker exchangeWorker,
        OrderBookBroadcaster broadcaster,
        ILogger<OrderBookWebSocketHandler> logger)
    {
        _exchangeWorker = exchangeWorker;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(HttpContext context, WebSocket clientSocket)
    {
        var cts = new CancellationTokenSource();
        var clientId = Guid.NewGuid();
        _logger.LogInformation("[WS] New connection: {ConnId} ({ClientId})", context.Connection.Id, clientId);

        if (!_exchangeWorker.IsRunning)
            _ = _exchangeWorker.StartAsync();

        var channel = _broadcaster.Subscribe(clientId);
        decimal? requestedBtcAmount = null;

        var sendTask = SendLoop(clientSocket, channel, cts.Token, () => requestedBtcAmount);
        var receiveTask = ReceiveLoop(context, clientSocket, cts, clientId, amount => requestedBtcAmount = amount);

        await Task.WhenAny(sendTask, receiveTask);
    }

    private async Task SendLoop(WebSocket socket, ChannelReader<string> channel, CancellationToken token, Func<decimal?> getBuyAmount)
    {
        try
        {
            await foreach (var raw in channel.ReadAllAsync(token))
            {
                BitstampEnvelope? env;
                try
                {
                    env = JsonSerializer.Deserialize<BitstampEnvelope>(raw, _jsonOpts);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WS] Deserialize failed for message: {Raw}", raw);
                    continue;
                }

                if (env?.Event?.Equals("data", StringComparison.OrdinalIgnoreCase) != true || env.Data == null)
                    continue;

                object payload = getBuyAmount() is decimal buy && buy > 0
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
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, token);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ReceiveLoop(HttpContext context, WebSocket socket, CancellationTokenSource cts, Guid clientId, Action<decimal> setAmount)
    {
        var buffer = new byte[2048];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (msg.Equals("close", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("[WS] Client {ConnId} requested close.", context.Connection.Id);
                    await _exchangeWorker.StopAsync();
                    break;
                }

                try
                {
                    using var doc = JsonDocument.Parse(msg);
                    if (doc.RootElement.TryGetProperty("buyAmount", out var amt))
                    {
                        if (amt.ValueKind == JsonValueKind.String &&
                            decimal.TryParse(amt.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1))
                            setAmount(d1);
                        else if (amt.ValueKind == JsonValueKind.Number && amt.TryGetDecimal(out var d2))
                            setAmount(d2);

                        _logger.LogInformation("[WS] Client {ConnId} provided buyAmount.", context.Connection.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WS] Failed to parse client message: {Msg}", msg);
                }
            }
        }
        finally
        {
            cts.Cancel();
            _broadcaster.Unsubscribe(clientId);
            _logger.LogInformation("[WS] Connection closed: {ConnId} ({ClientId})", context.Connection.Id, clientId);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
        }
    }
}
