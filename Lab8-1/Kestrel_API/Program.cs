var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway!");

app.MapGet("/api/status", () => new
{
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

app.MapGet("/api/led/{state}", (string state) =>
{
    var normalizedState = state.Trim().ToLowerInvariant();
    var isOn = normalizedState == "on";
    var action = isOn ? "TURN ON" : "TURN OFF";

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {normalizedState}");

    return Results.Ok(new
    {
        device = "LED_D2",
        requestedState = state,
        actionResult = action,
        serverTime = DateTime.Now.ToString("HH:mm:ss")
    });
});

app.MapGet("/api/student", () => new
{
    studentId = "67030116",
    studentName = "REPLACE_WITH_ENGLISH_NAME",
    faculty = "REPLACE_WITH_FACULTY_AND_PROGRAM",
    targetSensor = "Potentiometer",
    timestamp = DateTime.Now.ToString("O")
});

app.Run();
