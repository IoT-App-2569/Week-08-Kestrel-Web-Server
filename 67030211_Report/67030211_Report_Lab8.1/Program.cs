var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(opt => opt.ListenAnyIP(5000));
var app = builder.Build();
app.MapGet("/api/status", () => Results.Ok(new { status = "Healthy", uptime = Environment.TickCount64 / 1000 }));
app.MapGet("/api/telemetry", () => Results.Ok(new { voltage = 1.65, raw = 2048 }));
app.Run();
