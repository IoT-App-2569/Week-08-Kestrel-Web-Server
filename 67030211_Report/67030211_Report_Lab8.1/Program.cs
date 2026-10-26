var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5000));
var app = builder.Build();

app.MapGet("/", () => "Kestrel Minimal API Running!");
app.MapGet("/api/status", () => Results.Ok(new { status = "Healthy", uptime = Environment.TickCount64 / 1000 }));
app.MapGet("/api/sensor", () => Results.Ok(new { voltage = 1.65, raw = 2048 }));

app.Run();
