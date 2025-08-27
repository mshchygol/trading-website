using TradingApp.Endpoints;

namespace TradingApp.Extensions;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Register all app endpoints (REST + WebSocket).
    /// </summary>
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        // Order book endpoints
        app.MapOrderBookEndpoints();

        return app;
    }
}
