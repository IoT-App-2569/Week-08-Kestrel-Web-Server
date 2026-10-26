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
   - **คำตอบ** หน้าเว็บจะแสดงข้อผิดพลาด Failed to Load Page หรือ ERR_CONNECTION_REFUSED ไม่สามารถเข้าถึงเว็บไซต์ได้เนื่องจากกระบวนการการทำงานของเว็บเซิร์ฟเวอร์ถูกกยกเลิกและปิดตัวลงเมื่อกด Ctrl + C ทำให้ไม่มีบริการคอยรับฟัง (Listen) และตอบสนองคำขอ HTTP Request ที่พอร์ต 5288 เบราว์เซอร์จึงปฏิเสธการเชื่อมต่อ

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
 <img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/440ff93b-1cbb-4d2f-bf80-630d9d66de2f" />

<img width="1366" height="768" alt="image" src="https://github.com/user-attachments/assets/4ed518f6-c8a4-45d1-9d6a-735ef86211fe" />

---

## คำถามท้ายการทดลอง (Review Questions)
### 1. ในสถาปัตยกรรมของ Kestrel ตัวแปร `builder` ทำหน้าที่อะไร และตัวแปร `app` ทำหน้าที่อะไร
* **`builder` (`WebApplicationBuilder`):** ทำหน้าที่จัดเตรียมและลงทะเบียนบริการต่างๆ (Dependency Injection) อ่านค่า Configuration และตั้งค่าคอนฟิกของเว็บเซิร์ฟเวอร์ก่อนที่แอปพลิเคชันจะถูกสร้างขึ้น
* **`app` (`WebApplication`):** ทำหน้าที่เป็นตัวแอปพลิเคชันหลักที่สร้างเสร็จแล้ว ใช้จัดการ HTTP Request Pipeline (Middleware) และกำหนดเส้นทาง (Routing) เช่น `app.MapGet()` เพื่อคอยรับฟังและตอบสนองคำขอที่เข้ามา

---

### 2. เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ในมุมมองของงาน IoT Gateway

| ประเด็น | .NET Minimal API | LAMP Stack (Apache + PHP) |
| :--- | :--- | :--- |
| **ประสิทธิภาพ & ทรัพยากร** | ประสิทธิภาพสูง ประมวลผลไว กิน RAM น้อย เหมาะกับอุปกรณ์ Edge/IoT | ใช้ทรัพยากรสูงกว่า เนื่องจากต้องรันทั้ง Apache และ PHP Engine พร้อมกัน |
| **ความสะดวกในการพัฒนา** | เขียนโค้ดจบในไฟล์เดียว (`Program.cs`) มี Kestrel Web Server ในตัว | ต้องติดตั้งและตั้งค่า Apache/PHP และจัดวางโครงสร้างโฟลเดอร์ใน `htdocs` / `www` |
| **การรองรับ Real-time** | รองรับ Asynchronous และ WebSockets/SignalR ในตัว เหมาะกับข้อมูลเซนเซอร์ | ทำงานแบบ Synchronous เป็นหลัก ทำ Real-time Data Streaming ได้ยากกว่า |

---

### 3. การเพิ่ม `/api/` เข้าไปใน route มีประโยชน์อย่างไรบ้าง และถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง

* **ประโยชน์ของการใส่ `/api/`:**
  * **แยกประเภท Route ชัดเจน:** ช่วยแบ่งแยกระหว่างส่วนบริการข้อมูล (Data Endpoints / JSON) กับส่วนแสดงผลหน้าเว็บ (Static Content เช่น HTML/CSS/JS)
  * **จัดการ Security & Policy ได้ง่าย:** สามารถตั้งค่าระบบยืนยันตัวตน (Authentication) หรือนโยบาย CORS เฉพาะกลุ่มเส้นทาง `/api/*` ได้ในจุดเดียว
  * **ง่ายต่อการทำ Reverse Proxy:** ช่วยให้ Web Server หลัก (เช่น Nginx) ส่งคำขอที่ขึ้นต้นด้วย `/api/` ไปยัง Backend Server ได้ถูกต้อง

* **ปัญหาหากไม่ใส่ `/api/`:**
  * **เกิด Route ชนกัน (Naming Conflict):** หากมีหน้าเว็บ `/student` และต้องการทำ API ชื่อ `/student` เช่นกัน ระบบจะไม่สามารถแยกแยะความต้องการของผู้ใช้งานได้
  * **ยุ่งยากในการขยายระบบ:** โครงสร้างไม่เป็นสัดส่วน และยากต่อการทำ API Versioning (เช่น `/api/v1/student`) ในอนาคต
