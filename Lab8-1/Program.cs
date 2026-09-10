var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// กิจกรรมที่ 2: เปลี่ยนข้อความทักทาย
app.MapGet("/", () => "Welcome to IoT Edge Gateway by [rattapum sornkeaw]!");

// กิจกรรมที่ 3: ส่งคืน JSON สถานะ Gateway
app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

// กิจกรรมที่ 4: รับค่า URL Path ควบคุม LED และ Log บน Terminal
app.MapGet("/api/led/{state}", (string state) => {
    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";
    
    // แสดงสถานะบน Terminal
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
    studentId = "67030338", // แก้เป็นรหัสนักศึกษาของคุณ
    studentName = "rattapum sornkeaw", // แก้เป็นชื่อ-นามสกุลภาษาอังกฤษ
    faculty = "technology computer", // แก้เป็นคณะ/สาขา
    targetSensor = "DHT22", // แก้เป็นชื่อเซนเซอร์ที่สนใจ
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});

app.Run();