var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<SerialBridgeWorker>();
var app = builder.Build();
app.MapGet("/api/telemetry/latest", (GatewayStateStore store) => Results.Ok(store.Get("latest")));
app.Run();
