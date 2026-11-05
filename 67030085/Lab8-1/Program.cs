var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway by Thanut-orn Sripoohtron!");

app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

app.MapGet("/api/led/{state}", (string state) => {
    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {state}");
    return Results.Ok(new { 
        device = "LED_D2", 
        requestedState = state, 
        actionResult = action,
        serverTime = DateTime.Now.ToString("HH:mm:ss")
    });
});

app.MapGet("/api/student", () => {
    return Results.Ok(new {
        studentId = "67030085",
        studentName = "Thanut-orn Sripoohtron",
        faculty = "School of Industrial Education and Technology",
        targetSensor = "Soil Moisture Sensor",
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    });
});

app.Run();
