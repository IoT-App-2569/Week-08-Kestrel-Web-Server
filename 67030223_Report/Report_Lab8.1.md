# Labsheet 8.1: Kestrel Minimal API Fundamentals & JSON Serialization


#### 1. บันทึกผลการทดลอง / โค้ดที่พัฒนา

ไฟล์ `Program.cs` สำหรับรัน Kestrel Minimal API และสร้าง REST API สำหรับระบบ IoT Gateway:

```csharp
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
```

#### 2. ผลลัพธ์จาก Serial Monitor / หน้าจอแสดงผล

ผลลัพธ์บน Terminal เมื่อรันคำสั่ง `dotnet run` และเรียกใช้งาน Endpoint ต่างๆ:

```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/babyying/Desktop/App of IoT/Week-08/Lab8-1

[14:20:15] LED Control: on
[14:20:28] LED Control: off
```

ผลลัพธ์ที่ตอบกลับมาบนเบราว์เซอร์:

1. เมื่อเปิด URL `http://localhost:5000/`
```text
Welcome to IoT Edge Gateway by วิชญาพร เนียมเที่ยง!
```

2. เมื่อเปิด URL `http://localhost:5000/api/status`
```json
{
  "gateway": "ESP32-EdgeGateway",
  "status": "Online",
  "uptimeSeconds": 35,
  "isHealthy": true
}
```

3. เมื่อเปิด URL `http://localhost:5000/api/led/on`
```json
{
  "device": "LED_D2",
  "requestedState": "on",
  "actionResult": "TURN ON 💡",
  "serverTime": "14:20:15"
}
```

4. เมื่อเปิด URL `http://localhost:5000/api/student` (Micro-Challenge)
```json
{
  "studentId": "67030211",
  "studentName": "Witchayaporn Niamthiang",
  "faculty": "Faculty of Engineering",
  "targetSensor": "Potentiometer",
  "timestamp": "2026-09-06 14:21:05"
}
```

#### 3. สรุปผลและการตอบคำถามท้ายใบงาน

- **สรุปผลการทดลอง:**
  สามารถสร้างและรัน Kestrel Web Server ด้วย .NET Minimal API ได้สำเร็จ สามารถส่งข้อมูลตอบกลับทั้งในรูปแบบข้อความธรรมดาและโครงสร้างข้อมูลแบบ JSON โดยที่ .NET ทำการแปลงออบเจกต์ (Serialize) ให้โดยอัตโนมัติ และสามารถสร้าง Endpoint รับพารามิเตอร์ผ่าน URL Path เพื่อสั่งการอุปกรณ์พร้อมแสดงสถานะบนเทอร์มินัลได้ถูกต้อง

- **[Checkpoint 1.1 ทดสอบความเข้าใจ]**
  - **คำถาม:** ในหน้าจอ Terminal ขณะที่เซิร์ฟเวอร์กำลังรันอยู่ ให้กดปุ่ม `Ctrl + C` เพื่อหยุดโปรแกรม กลับไปที่หน้าเบราว์เซอร์แล้วกดปุ่ม Refresh (F5) สังเกตว่าเกิดอะไรขึ้น และอธิบายสั้นๆ ว่าทำไมจึงเป็นเช่นนั้น
  - **คำตอบ:** หน้าเว็บแสดงข้อความ `This site can’t be reached` หรือ `ERR_CONNECTION_REFUSED` เนื่องจากโปรเซสของ Kestrel Web Server ถูกยกเลิกและปิดพอร์ตลง ทำให้เบราว์เซอร์ไม่สามารถสร้างการเชื่อมต่อเครือข่าย TCP กับพอร์ตของเซิร์ฟเวอร์ได้

- **คำถามท้ายการทดลอง (Review Questions):**

  1. **ในสถาปัตยกรรมของ Kestrel ตัวแปร `builder` ทำหน้าที่อะไร และตัวแปร `app` ทำหน้าที่อะไร**
     - **คำตอบ:** builder (WebApplicationBuilder): ทำหน้าที่เตรียมและกำหนดค่าก่อนเริ่มระบบ เช่น ลงทะเบียน Services ลงใน Dependency Injection (DI) Container, กำหนดค่า Configuration และจัดการ Logging

app (WebApplication): ทำหน้าที่เป็นตัวแอปพลิเคชันหลักที่สร้างเสร็จแล้ว ใช้สำหรับกำหนด Routing/Endpoints (เช่น MapGet), จัดการ HTTP Request Pipeline (Middleware) และสั่งรันเซิร์ฟเวอร์ Kestrel ด้วย app.Run()

2. **เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway**
     - **คำตอบ:** .NET Minimal API:

ข้อดี: ประสิทธิภาพและ Throughput สูง กินทรัพยากรน้อย โค้ดทั้งหมดรวมอยู่ในไฟล์เดียวแบบ Single Binary ทำให้ Build และ Deploy บนอุปกรณ์ Edge/Gateway (เช่น Raspberry Pi) ได้ง่าย

ข้อเสีย: ต้องการ .NET Runtime ในการรัน และหากระบบขยายใหญ่ขึ้น การจัดการสถาปัตยกรรมโค้ดในไฟล์เดียวอาจซับซ้อนขึ้น

LAMP Stack (Apache + PHP):

ข้อดี: ติดตั้งง่าย เครื่องมือบริหารจัดการแพร่หลาย แก้ไขสคริปต์ PHP แล้วทดสอบได้ทันทีโดยไม่ต้องคอมไพล์ใหม่

ข้อเสีย: สถาปัตยกรรมหนักกว่า ใช้ RAM และ CPU สูงกว่า ไม่เหมาะกับงาน Edge Gateway ขนาดเล็กที่ต้องรับส่งข้อมูลความถี่สูงแบบ Real-time

 3. **นักศึกษาคิดว่าการเพิ่ม `/api/` เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง**
     - **คำตอบ:** ประโยชน์ของการใส่ /api/:

แยกประเภท Route ชัดเจน: แยกความแตกต่างระหว่าง Endpoint ที่คืนข้อมูลดิบ (Data API เช่น JSON สำหรับ IoT หรือแอปพลิเคชัน) ออกจากหน้าเว็บที่แสดงผล UI (เช่น HTML/Static Files)

จัดการ Reverse Proxy / Routing ง่าย: ตั้งค่ากฎบน Reverse Proxy หรือ API Gateway (เช่น Nginx, Traefik) ให้ Forward หรือ Caching กลุ่มคำขอ /api/* ไปยัง Backend Service ได้สะดวก

ทำ Versioning และความปลอดภัยง่าย: กำหนดสิทธิ์ (Middleware/Auth) หรือเพิ่มเวอร์ชัน (เช่น /api/v1/) ได้เป็นระบบโดยไม่กระทบ URL อื่น

ปัญหาหากไม่ใส่ /api/:

เสี่ยงเกิด Path ชนกัน (Route Collision): Route ของ API อาจซ้ำกับชื่อไฟล์ Static หรือ Route หน้าเว็บ (เช่น ฝั่งหน้าเว็บมีหน้า /status แล้ว API ก็ใช้ /status ตรงกัน)

จัดระเบียบสิทธิ์ยาก: กำหนด Middleware กลางเพื่อคุมความปลอดภัย เช่น จำกัด Rate Limit หรือตรวจ API Key เฉพาะกลุ่ม Endpoint บริการข้อมูลได้ยากขึ้น