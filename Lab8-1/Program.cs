var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// กิจกรรมที่ 2: ข้อความต้อนรับ
app.MapGet("/", () => "Welcome to IoT Edge Gateway by Kanruethai Kaewsawang [67030021]!");

// กิจกรรมที่ 3: แปลง C# Object เป็น JSON อัตโนมัติ
app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

// กิจกรรมที่ 4: การรับค่าผ่าน URL Path (Route Parameters)
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

// ภารกิจท้าทาย (Micro-Challenge): ข้อมูลนักศึกษา
app.MapGet("/api/student", () => Results.Ok(new {
    studentId = "67030021",
    studentName = "Kanruethai Kaewsawang",
    faculty = "SIET",
    targetSensor = "DHT22",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
}));

app.Run();
