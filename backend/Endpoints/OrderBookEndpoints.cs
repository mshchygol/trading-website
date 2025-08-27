using System.Text.Json;
using TradingApp.Services;

namespace TradingApp.Endpoints;

public static class OrderBookEndpoints
{
    public static void MapOrderBookEndpoints(this IEndpointRouteBuilder app)
    {
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
            var handler = context.RequestServices.GetRequiredService<IOrderBookWebSocketHandler>();

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await handler.HandleConnectionAsync(context, socket);
        });
    }
}
