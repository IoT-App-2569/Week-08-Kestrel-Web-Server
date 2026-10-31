var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway by [67030172-pattarapon]!");

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

app.MapGet("/api/student", () => new {
    studentId = "67030172",
    studentName = "pattarapon", // แก้เป็นชื่อ-นามสกุลภาษาอังกฤษของตัวเอง
    faculty = "เทคโนโลยีคอมพิวเตอร์", // แก้เป็นคณะ/สาขาของตัวเอง
    targetSensor = "Potentiometer", // แก้เป็นเซนเซอร์ที่สนใจ เช่น "Potentiometer", "MQ-2"
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});


app.Run();
