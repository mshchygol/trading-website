
using YourApp.Extensions;
using YourApp.Services;

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

var app = builder.Build();

app.UseCors();
app.UseWebSockets();

app.MapGet("/", () => "Hello World!");

// Register endpoints via extension
app.MapAppEndpoints();

app.Run();
