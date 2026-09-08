// ============================================================================
//  ใบงานการทดลองที่ 8.1 : พื้นฐาน Kestrel Web Server และ Minimal API
//  รหัสนักศึกษา : 67030098
//  ----------------------------------------------------------------------------
//  Kestrel เป็นเว็บเซิร์ฟเวอร์ที่ "ฝังตัว" อยู่ในโปรเซสนี้เอง (Self-Hosted)
//  ไม่ต้องติดตั้ง Apache / Nginx / PHP แยกต่างหากเหมือน LAMP Stack
// ============================================================================

using System.Globalization;

// --- ช่วงประกอบร่าง (Configuration Phase) -----------------------------------
// builder ทำหน้าที่อ่านคอนฟิก ตั้งค่า Logging และลงทะเบียน Service เข้า DI Container
var builder = WebApplication.CreateBuilder(args);

// --- ช่วงทำงานจริง (Runtime Phase) ------------------------------------------
// เมื่อเรียก Build() แล้วจะเพิ่ม Service เข้า builder.Services ไม่ได้อีก
var app = builder.Build();


// ============================================================================
//  กิจกรรมที่ 2 : หน้าแรก — คืนค่าเป็นข้อความธรรมดา (text/plain)
// ============================================================================
app.MapGet("/", () => "Welcome to IoT Edge Gateway by 67030098!");


// ============================================================================
//  กิจกรรมที่ 3 : Anonymous Object -> JSON อัตโนมัติ (Zero-Config Serialization)
//  ----------------------------------------------------------------------------
//  สังเกตว่าเราไม่ได้เรียก json_encode() หรือแปลงสตริงเองเลย
//  Kestrel จะ Serialize ออบเจกต์ให้เป็น JSON และแปะ Header
//  Content-Type: application/json ให้อัตโนมัติ
// ============================================================================
app.MapGet("/api/status", () => new
{
    gateway = "ESP32-EdgeGateway",
    status = "Online",

    // TickCount64 = เวลาที่เครื่องเปิดมาแล้ว (มิลลิวินาที) หารพันเพื่อให้เป็นวินาที
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});


// ============================================================================
//  กิจกรรมที่ 4 : รับค่าผ่าน URL Path (Route Parameter)
//  ----------------------------------------------------------------------------
//  ส่วน {state} ใน route จะถูก Model Binding ผูกเข้ากับพารามิเตอร์ string state
//  ตัวอย่างการเรียก : /api/led/on  และ  /api/led/off
// ============================================================================
app.MapGet("/api/led/{state}", (string state) =>
{
    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";

    // พิมพ์ที่ฝั่งเซิร์ฟเวอร์ (Server-Side) ไม่ใช่ที่เบราว์เซอร์
    // เป็นหลักฐานว่าโค้ดทำงานอยู่บนเครื่องเซิร์ฟเวอร์จริง
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {state}");

    return Results.Ok(new
    {
        device = "LED_D2",
        requestedState = state,
        actionResult = action,
        serverTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
    });
});


// ============================================================================
//  ภารกิจท้าทาย (Micro-Challenge) : ข้อมูลประจำตัวนักศึกษา
//  ----------------------------------------------------------------------------
//  studentId ต้องเป็น "สตริง" ตามโจทย์ — ถ้าประกาศเป็น int เลข 0 นำหน้าจะหายไป
//  และรหัสนักศึกษาเป็นรหัสประจำตัว ไม่ใช่ตัวเลขที่ต้องนำไปคำนวณ
// ============================================================================
app.MapGet("/api/student", () => new
{
    studentId = "67030098",
    studentName = "Theeranat Phutiwanich",
    faculty = "School of Industrial Education and Technology. Tehcnology Computer",
    targetSensor = "Potentiometer",

    // ต้องระบุ InvariantCulture เสมอ มิฉะนั้นบนเครื่องที่ตั้งภาษาไทย (th-TH)
    // ปฏิทินเริ่มต้นจะเป็นพุทธศักราช ทำให้ได้ปี 2569 แทนที่จะเป็น 2026
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
});


// app.Run() สั่งให้ Kestrel เริ่ม listen และ "บล็อกเธรดหลัก" ไว้จนกว่าจะกด Ctrl+C
// เมื่อโปรเซสนี้ตาย พอร์ต TCP จะถูกคืนทันที เบราว์เซอร์จะขึ้น ERR_CONNECTION_REFUSED
app.Run();
