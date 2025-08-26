using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using YourApp.Endpoints;

namespace YourApp.Extensions;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Register all app endpoints (REST + WebSocket).
    /// </summary>
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        // Order book endpoints
        app.MapOrderBookEndpoints();

        // You can add other endpoint groups here in the future:
        // app.MapHealthCheckEndpoints();
        // app.MapUserEndpoints();

        return app;
    }
}
