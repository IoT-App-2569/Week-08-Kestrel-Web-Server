var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// กิจกรรมที่ 2: เปลี่ยนข้อความทักทายเป็นชื่อนักศึกษา
app.MapGet("/", () => "Welcome to IoT Edge Gateway by Krittanai Boonnoy (67030011)!");

// กิจกรรมที่ 3: ส่งคืน C# Anonymous Object -> Kestrel แปลงเป็น JSON ให้อัตโนมัติ
app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

// กิจกรรมที่ 4: รับค่าผ่าน URL Path (Route Parameter)
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

// Micro-Challenge: ข้อมูลนักศึกษา
app.MapGet("/api/student", () => new {
    studentId = "67030011",
    studentName = "Krittanai Boonnoy",
    faculty = "SIET",
    targetSensor = "Potentiometer",
    // ใช้ InvariantCulture เพราะเครื่องที่ตั้ง Locale ไทยจะได้ปี พ.ศ. (2569) แทน ค.ศ.
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
});

app.Run();
