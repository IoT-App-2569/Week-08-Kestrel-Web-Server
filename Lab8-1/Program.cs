var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ===== Endpoint ควบคุม LED (ของเดิมในโจทย์) =====
app.MapGet("/api/led/{state}", (string state) =>
{
    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {state}");
    return Results.Ok(new
    {
        device = "LED_D2",
        requestedState = state,
        actionResult = action,
        serverTime = DateTime.Now.ToString("HH:mm:ss")
    });
});

// ===== Endpoint ข้อมูลนักศึกษา =====
app.MapGet("/api/student", () =>
{
    return Results.Ok(new
    {
        studentId = "67030052",                                   // แก้เป็นรหัสนักศึกษาของคุณ (ต้องเป็นสตริง)
        studentName = "CHAYANGGOON SONGPHIM",                        // แก้เป็นชื่อ-นามสกุลภาษาอังกฤษ
        faculty = "Faculty of Industrial Education and Technology", // แก้เป็นคณะและสาขาวิชาของคุณ
        targetSensor = "DHT22",                                   // แก้เป็นเซนเซอร์ที่สนใจ เช่น "Potentiometer", "MQ-2"
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    });
});

app.Run();