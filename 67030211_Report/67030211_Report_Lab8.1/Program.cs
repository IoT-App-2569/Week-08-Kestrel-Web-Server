var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// กิจกรรมที่ 2: หน้าแรกแสดงข้อความทักทาย
app.MapGet("/", () => "Welcome to IoT Edge Gateway by วิชญาพร เนียมเที่ยง!");

// กิจกรรมที่ 3: ส่งคืนสถานะระบบเป็น JSON อัตโนมัติ
app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

// กิจกรรมที่ 4: รับค่าพารามิเตอร์ผ่าน Route เพื่อควบคุมอุปกรณ์
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

// ภารกิจท้าทาย (Micro-Challenge): ข้อมูลนักศึกษาและเซนเซอร์
app.MapGet("/api/student", () => Results.Ok(new {
    studentId = "67030211",
    studentName = "Witchayaporn Niamthiang",
    faculty = "Faculty of Engineering",
    targetSensor = "Potentiometer",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
}));

app.Run();
