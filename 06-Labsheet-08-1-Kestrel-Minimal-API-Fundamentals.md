# ใบงานการทดลองที่ 8.1 (Labsheet 8.1)
## พื้นฐาน Kestrel Web Server และ Minimal API

> **คำชี้แจง** ใบงานนี้มุ่งเน้นให้นักศึกษาได้สัมผัสประสบการณ์การสร้าง High-Performance Web Server ด้วยตนเองผ่านกระบวนการ *"ได้ทดลอง ได้เห็นผลลัพธ์ทีละว้าว"* โดยจะค่อยๆ สร้างโค้ดทีละสเต็ปสั้นๆ ห้ามคัดลอกโค้ดก้อนใหญ่ เพื่อสร้างความเข้าใจที่แท้จริง

---
## วัตถุประสงค์การทดลอง (Objectives)
1. สามารถติดตั้งและตรวจสอบสภาพแวดล้อมการทำงานของ .NET 8 / 9 SDK ได้
2. สามารถสร้างและรัน Kestrel Web Server ด้วยคำสั่ง .NET CLI ได้อย่างถูกต้อง
3. เข้าใจโครงสร้างของไฟล์โปรเจกต์ `.csproj` และไฟล์โค้ดหลัก `Program.cs`
4. สามารถสร้าง Minimal API Endpoint ที่ส่งคืนข้อมูลแบบสตริงธรรมดาและออบเจกต์ JSON อัตโนมัติได้
5. สามารถเขียน Endpoint รับค่าพารามิเตอร์ผ่าน URL Path ได้ด้วยตนเอง

---
## เครื่องมือและสิ่งที่ต้องเตรียม (Prerequisites)
- คอมพิวเตอร์ระบบปฏิบัติการ Windows / macOS / Linux
- ติดตั้ง **.NET SDK 8.0** หรือใหม่กว่า
- โปรแกรม **Visual Studio Code (VS Code)**
- เว็บเบราว์เซอร์ (Google Chrome, Microsoft Edge)

---

### ใบงานย่อยที่ 8-1 การสร้างเว็บไซต์อย่างง่ายด้วย Kestrel  
#### กิจกรรมที่ 1 การตรวจสอบความพร้อมของระบบและสร้างโปรเจกต์ 

1. เปิดโปรแกรม **Terminal** หรือ **PowerShell** ใน VS Code แล้วพิมพ์คำสั่งตรวจสอบเวอร์ชัน
   ```bash
   dotnet --version
   ```
   > *ผลลัพธ์ควรแสดงเป็นตัวเลข เช่น `8.0.xxx`,   `9.0.xxx`  หรือ 10.0.xxx*

2. สร้างโฟลเดอร์สำหรับการทดลองใบงานย่อยที่ 8.1 และสร้างโปรเจกต์ Web ว่างเปล่า (Empty Web)
   ```bash
   # สร้างโฟลเดอร์และเข้าไปด้านใน
   mkdir Lab8-1 && cd Lab8-1

   # สร้างโปรเจกต์ Web ว่างเปล่า
   dotnet new web -o .
   ```

3. ทดลองสั่งรันเว็บเซิร์ฟเวอร์
   ```bash
   dotnet run
   ```
   สังเกตข้อความบน Terminal จะพบข้อความแจ้งว่าเซิร์ฟเวอร์เริ่มทำงานแล้ว
   ```text
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://localhost:5xxx
   ```
โดย 5xxx จะเป็นเลข 4 หลักที่ระบบสร้างมาให้

4. เปิดเบราว์เซอร์แล้วพิมพ์ URL: `http://localhost:5xxx`  
   หรือกด ctrl + click ที่บรรทัด `Now listening on: http://localhost:5xxx`
   **สิ่งที่เห็น** คำว่า **"Hello World!"** ปรากฏบนหน้าจอ

> สังเกตว่าเราไม่ต้องติดตั้ง Apache, ไม่ต้องรัน Nginx, ไม่ต้องคอนฟิก PHP เลยแม้แต่น้อย โปรแกรม `.exe` ที่เราเขียนมี Kestrel Web Server ฝังตัวอยู่แล้วในกระบวนการ

---

#### [Checkpoint 1.1 ทดสอบความเข้าใจ]


1. ในหน้าจอ Terminal ขณะที่เซิร์ฟเวอร์กำลังรันอยู่ ให้กดปุ่ม `Ctrl + C` เพื่อหยุดโปรแกรม
2. กลับไปที่หน้าเบราว์เซอร์แล้วกดปุ่ม **Refresh (F5)** สังเกตว่าเกิดอะไรขึ้น และอธิบายสั้นๆ ว่าทำไมจึงเป็นเช่นนั้น
   - **คำตอบ** เบราว์เซอร์จะแสดงหน้าข้อความผิดพลาด เช่น "Unable to connect", "Site can't be reached" หรือ "Connection refused"
เหตุผล เนื่องจากกระบวนการทำงานของ Kestrel Web Server ถูกสั่งหยุดลงด้วยคำสั่ง Ctrl + C ทำให้ไม่มีบริการ (Service) คอยดักฟัง (Listen) และตอบรับคำร้องขอ (HTTP Request) จากเบราว์เซอร์อีกต่อไป
---

#### กิจกรรมที่ 2 โครงสร้างและเขียนโค้ด

1. เปิดไฟล์ `Program.cs` ขึ้นมาดู จะพบว่ามีโค้ดเพียงไม่กี่บรรทัด
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   var app = builder.Build();

   app.MapGet("/", () => "Hello World!");

   app.Run();
   ```

2. ทดลองเปลี่ยนข้อความทักทายเป็นชื่อของนักศึกษาเอง เช่น
   ```csharp
   app.MapGet("/", () => "Welcome to IoT Edge Gateway by [ใส่ชื่อ-นามสกุลนักศึกษา]!");
   ```

3. บันทึกไฟล์ สั่ง `dotnet run` อีกครั้ง แล้วกด Refresh บนเบราว์เซอร์เพื่อดูผลลัพธ์

---

#### กิจกรรมที่ 3 การแปลง C# Object เป็น JSON โดยอัตโนมัติ 

ในระบบ IoT การสื่อสารส่วนใหญ่ใช้รูปแบบข้อมูล **JSON (JavaScript Object Notation)** มาดูกันว่า .NET จัดการเรื่องนี้ให้เราง่ายแค่ไหน

1. เปิดไฟล์ `Program.cs` แล้วเพิ่มโค้ด Endpoint ใหม่เข้าไป **ก่อนบรรทัด `app.Run();`**
   ```csharp
   app.MapGet("/api/status", () => new {
       gateway = "ESP32-EdgeGateway",
       status = "Online",
       uptimeSeconds = Environment.TickCount64 / 1000,
       isHealthy = true
   });
   ```

2. สั่ง `dotnet run` จากนั้นเปิดเบราว์เซอร์ไปที่  
     `http://localhost:5000/api/status`

3. **สิ่งที่เห็น** หน้าจอเบราว์เซอร์จะแสดงผลเป็น JSON โครงสร้างสมบูรณ์
   ```json
   {
     "gateway": "ESP32-EdgeGateway",
     "status": "Online",
     "uptimeSeconds": 25,
     "isHealthy": true
   }
   ```

> เราไม่ได้สั่ง `json_encode()` หรือแปลงสตริงเลย เพียงแค่เราส่ง C# Anonymous Object ออกมา Kestrel จะทำการ Serialize เป็น JSON และแปะ Header `Content-Type: application/json` ให้อัตโนมัติ!

---

#### กิจกรรมที่ 4 การรับค่าผ่าน URL Path (Route Parameters)

เพิ่มฟังก์ชันการรับคำสั่งควบคุม เช่น สั่งเปิด-ปิดหลอดไฟ LED หรือรีเลย์ผ่าน URL

1. เพิ่ม Endpoint ต่อไปนี้ลงใน `Program.cs`
   ```csharp
   app.MapGet("/api/led/{state}", (string state) => {
       string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";
       return Results.Ok(new { 
           device = "LED_D2", 
           requestedState = state, 
           actionResult = action,
           serverTime = DateTime.Now.ToString("HH:mm:ss")
       });
   });
   ```

2. ทดสอบเปิด URL บนเบราว์เซอร์
   - ทดสอบพิมพ์ `http://localhost:5000/api/led/on`
   - ทดสอบพิมพ์ `http://localhost:5000/api/led/off`
   - สังเกตผลลัพธ์ JSON ที่ได้รับกลับมา


เพิ่มบรรทัด 
```csharp
	Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {state}");
```
ถัดจากบรรทัด
```csharp
	    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";   
```

เพื่อแสดงสถานะของ LED ที่ terminal

จากนั้นทดลองเปลี่ยนข้อความที่ URL bar แล้วสังเกตุและบันทึกผลที่ terminal
**ผลลัพธ์ที่คาดหวังบนเทอร์มินอล**
```
[xx:xx:xx] LED Control: on
[xx:xx:xx] LED Control: off
```


---

## ภารกิจท้าทาย (Micro-Challenge)

ให้นักศึกษาเขียน Endpoint ของตัวเองเพิ่มลงใน `Program.cs` ตามเงื่อนไขดังนี้

1. ตั้งชื่อ Path ว่า `/api/student`
2. เมื่อเปิดเข้าไปดู จะต้องคืนค่า JSON ที่มีข้อมูลดังต่อไปนี้
   - `studentId` = รหัสนักศึกษาของตนเอง (ชนิดสตริง)
   - `studentName`= ชื่อ-นามสกุลภาษาอังกฤษของตนเอง
   - `faculty`= คณะและสาขาวิชาที่กำลังศึกษา
   - `targetSensor`= ชื่อเซนเซอร์ที่ตนเองสนใจนำมาต่อกับ ESP32 ในวิชานี้ (เช่น "DHT22", "Potentiometer", "MQ-2")
   - `timestamp`= เวลาปัจจุบันของเซิร์ฟเวอร์ (`DateTime.Now.ToString(...)`)

 **หลักฐานการส่งงาน** บันทึกภาพหน้าจอเบราว์เซอร์ที่เปิดแสดงผล JSON จาก `/api/student` พร้อมโค้ดใน VS Code ลงในรายงานผลการทดลอง

```
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway by [นูรีน ปิ่นคล้าย]!");

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
    studentId = "67030120", 
    studentName = "Nurin Pinklai",
    faculty = "School of Industrial Education and Technology", 
    targetSensor = "DHT22",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});

app.Run();

```
<img width="937" height="976" alt="image" src="https://github.com/user-attachments/assets/247ec1f5-4251-4654-9068-55cb4cfbc8c1" />

---

## คำถามท้ายการทดลอง (Review Questions)
1. ในสถาปัตยกรรมของ Kestrel ตัวแปร `builder` ทำหน้าที่อะไร และตัวแปร `app` ทำหน้าที่อะไร
ตอบ builder (WebApplicationBuilder): ทำหน้าที่เตรียมและกำหนดค่าสภาพแวดล้อม (Setup & Configuration) รวมถึงการลงทะเบียนบริการต่างๆ (Services Registration / Dependency Injection) ก่อนที่เซิร์ฟเวอร์จะถูกสร้างขึ้นจริง

app (WebApplication): ทำหน้าที่เป็นตัว Web Server และ Application Instance ที่สร้างเสร็จแล้ว ใช้สำหรับกำหนดเส้นทางการรับส่งข้อมูล (HTTP Routing/Endpoints เช่น app.MapGet()), กำหนด Middleware และสั่งเริ่มการทำงานของเซิร์ฟเวอร์ผ่านคำสั่ง app.Run() 

2. เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway
ตอบ .NET Minimal API

ข้อดี: ตัว Web Server (Kestrel) ถูกฝังรวมมาในแอปพลิเคชัน (Self-Hosted/Embedded) ทำให้กินทรัพยากรน้อย โหลดเร็ว ประมวลผลแบบ Asynchronous ได้มีประสิทธิภาพสูง เหมาะกับการรับข้อมูลมหาศาลจากอุปกรณ์ IoT แบบ Real-time เขียนโค้ดสั้นและ Deploy ง่าย (เป็นไฟล์ Binary เดี่ยวๆ ได้)

ข้อเสีย: หากต้องการแสดงผล UI หน้าเว็บแบบซับซ้อนมากๆ ต้องเขียนแยกส่วน (เช่น React/Vue) หรือต้องตั้งค่าแยกจาก API

LAMP Stack (Apache + PHP)

ข้อดี: ติดตั้งและใช้งานสำหรับเว็บทั่วไปได้ง่าย มี Ecosystem และโมดูลสำเร็จรูปเยอะ จัดการไฟล์ HTML/PHP แบบผสมผสานได้สะดวก

ข้อเสีย: โครงสร้างการทำงานเทอะทะและกินทรัพยากรสูงกว่า ต้องรันผ่าน Web Server แยกต่างหาก (Apache) ทำให้การตอบสนองข้อมูล Real-time หรือการจัดการเซสชันของอุปกรณ์ IoT จำนวนมากทำได้ช้ากว่า .NET

3. นักศึกษาคิดว่าการเพิ่ม `/api/` เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง
ตอบ คำตอบสำหรับ Review Question ข้อ 3:

ประโยชน์ของการใส่ /api/ ใน Route:

จัดหมวดหมู่ระบบได้ชัดเจน (Separation of Concerns): ช่วยแยกแยะระหว่าง Endpoint ที่ใช้ส่งคืนข้อมูล (Data API / JSON) สำหรับอุปกรณ์ IoT หรือระบบหลังบ้าน ออกจาก Route ที่ใช้แสดงผลหน้าเว็บปกติ (Frontend UI / HTML)

ง่ายต่อการกำหนดสิทธิ์และความปลอดภัย (Security & CORS): สามารถตั้งค่า Middleware, การยืนยันตัวตน (Authentication), หรือนโยบายข้ามโดเมน (CORS) ครอบเฉพาะส่วนที่เป็น /api/* ได้ง่าย

รองรับการจัดการเวอร์ชัน (Versioning): สะดวกต่อการขยายระบบในอนาคต เช่น การเปลี่ยนเป็น /api/v1/ หรือ /api/v2/ โดยไม่กระทบกับโครงสร้างส่วนอื่น

ปัญหาที่อาจเกิดขึ้นถ้าไม่ใส่ /api/:

เกิด Route Collision (การชนกันของ Path): เสี่ยงที่ชื่อ Path ของ API จะไปซ้ำกับชื่อไฟล์ หรือชื่อหน้าเว็บปกติ (เช่น /status อาจจะซ้ำกับหน้าแสดงเว็บปกติ)

สับสนในการพัฒนาและดูแลรักษา: นักพัฒนาหรือระบบอื่นที่มาเชื่อมต่อ จะแยกไม่ออกว่า URL ไหนเป็นบริการส่งข้อมูล หรือ URL ไหนเป็นหน้าจอ UI

ตั้งค่า Reverse Proxy / Load Balancer ยากขึ้น: เวลาคลังระบบต้องจัดเส้นทางTraffic ในระดับ Server (เช่น Nginx) จะทำได้ยาก เพราะไม่สามารถใช้ Prefix /api/ ในการส่ง Traffic ไปยังระบบ backend IoT ได้โดยตรง
