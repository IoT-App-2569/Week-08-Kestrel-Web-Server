var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway by [Phumipat Kunduang]!");

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

// Endpoint ใหม่สำหรับข้อมูลนักศึกษา
app.MapGet("/api/student", () => new {
    studentId = "67030181", // รหัสนักศึกษาของคุณ
    studentName = "Phumipat Kunduang", // ชื่อ-นามสกุล
    faculty = "คณะครุศาสตร์อุตสาหกรรมและเทคโนโลยี สาขาเทคโนโลยีคอมพิวเตอร์", // ใส่คณะและสาขาวิชา
    targetSensor = "DHT22", // ใส่เซนเซอร์ที่สนใจ เช่น "DHT22", "MQ-2", "Potentiometer"
    timestamp = DateTime.Now.ToString("2026-09-07")
});

app.Run();