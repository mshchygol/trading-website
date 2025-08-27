
using TradingApp.Endpoints;
using TradingApp.Extensions;
using TradingApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Config
var frontendAppUrl = builder.Configuration["AppSettings:FrontendAppUrl"]!;

// Services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(frontendAppUrl).AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton<ExchangeIngestWorker>();
builder.Services.AddSingleton<OrderBookBroadcaster>();
builder.Services.AddSingleton<IOrderBookWebSocketHandler, OrderBookWebSocketHandler>();

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

// Register endpoints via extension
app.MapAppEndpoints();

app.Run();
