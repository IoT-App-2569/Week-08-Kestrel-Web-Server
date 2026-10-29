var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// กิจกรรมที่ 2
app.MapGet("/", () => "Welcome to IoT Edge Gateway by Kittikarn!");

// กิจกรรมที่ 3
app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

// กิจกรรมที่ 4
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

// ภารกิจท้าทาย (Micro-Challenge)
app.MapGet("/api/student", () => new {
    studentId = "67030023",
    studentName = "Kittikarn",
    faculty = "Faculty of Science and Technology, Computer Science and Digital Systems Engineering",
    targetSensor = "DHT22",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});

app.Run();