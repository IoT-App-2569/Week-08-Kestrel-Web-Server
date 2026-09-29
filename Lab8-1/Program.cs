var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway by นวพล สวัสดี-67030109");

app.MapGet("/api/status", () => new
{
	gateway = "ESP32-EdgeGateway",
	status = "Online",
	uptimeSeconds = Environment.TickCount64 / 1000,
	isHealthy = true
});

app.Run();
