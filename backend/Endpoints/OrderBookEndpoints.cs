using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Globalization;
using YourApp.Services;
using YourApp.Models;

namespace YourApp.Endpoints;

public static class OrderBookEndpoints
{
    public static void MapOrderBookEndpoints(this IEndpointRouteBuilder app)
    {
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // REST endpoint
        app.MapGet("/auditlog", () =>
            Results.Json(
                AuditLog.GetAll().Select(x => new { timestamp = x.Timestamp, snapshot = x.Snapshot }),
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        // WebSocket endpoint
        app.Map("/ws/orderbook", async context =>
        {
            var exchangeWorker = context.RequestServices.GetRequiredService<ExchangeIngestWorker>();

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var clientSocket = await context.WebSockets.AcceptWebSocketAsync();
            var cts = new CancellationTokenSource();
            var clientId = Guid.NewGuid();
            Console.WriteLine($"[WS] New connection: {context.Connection.Id} ({clientId})");

            if (!exchangeWorker.IsRunning)
                _ = exchangeWorker.StartAsync();

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
                            Console.WriteLine("[WS] Deserialize failed");
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
                                if (amt.ValueKind == JsonValueKind.String &&
                                    decimal.TryParse(amt.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1))
                                    requestedBtcAmount = d1;
                                else if (amt.ValueKind == JsonValueKind.Number && amt.TryGetDecimal(out var d2))
                                    requestedBtcAmount = d2;

                                if (requestedBtcAmount is decimal p)
                                    Console.WriteLine($"[WS] Client {context.Connection.Id} provided buyAmount: {p} BTC");
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
                    OrderBookBroadcaster.Unsubscribe(clientId);
                    Console.WriteLine($"[WS] Connection closed: {context.Connection.Id} ({clientId})");
                    await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                }
            });

            await Task.WhenAny(sendTask, receiveTask);
        });
    }
}
