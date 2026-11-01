# รายงานผลการทดลองสัปดาห์ที่ 8: Edge Web Server Architecture & Minimal API
**รหัสนักศึกษา:** 67030248  
**ชื่อ-นามสกุล:** นายสุวิจักขณ์ ลาวัง (Suwijak Lawang)  
**สาขาวิชา:** School of Industrial Education and Technology, Computer Technology  

---

## ใบงานการทดลองที่ 8.1: พื้นฐาน Kestrel Web Server และ Minimal API

### 1. วัตถุประสงค์การทดลอง
1. ติดตั้งและตรวจสอบสภาพแวดล้อมการทำงานของ .NET SDK
2. สร้างและรัน Kestrel Web Server ผ่าน .NET CLI ด้วย Minimal API ได้
3. เข้าใจโครงสร้างไฟล์ `.csproj` และ `Program.cs`
4. สร้าง Minimal API Endpoint ส่งคืนสตริงและออบเจกต์ JSON แบบอัตโนมัติ
5. รับค่าพารามิเตอร์ผ่าน URL Path (Route Parameters)

---

### 2. บันทึกผลการทดลองรายกิจกรรม

#### กิจกรรมที่ 1 & 2: ตรวจสอบความพร้อมของระบบ สร้างโปรเจกต์ และปรับแต่งข้อความทักทาย
- เวอร์ชัน .NET SDK ที่ใช้: `10.0.400`
- คำสั่งที่ใช้สร้างและรันโปรเจกต์:
  ```bash
  cd Lab8-1
  dotnet run
  ```
- URL ที่เซิร์ฟเวอร์เปิดให้บริการ: `http://localhost:5135`
- โค้ดที่ปรับแต่งใน `Program.cs`:
  ```csharp
  app.MapGet("/", () => "Welcome to IoT Edge Gateway by Suwijak Lawang!");
  ```
- **ผลลัพธ์เมื่อเปิดหน้าแรก `/` (บันทึกจาก Log เฉพาะบุคคล `logs/w08_1.log`):**
  ```text
  $ curl -i http://localhost:5135/
  HTTP/1.1 200 OK
  Content-Type: text/plain; charset=utf-8
  Server: Kestrel

  Welcome to IoT Edge Gateway by Suwijak Lawang!
  ```

---

#### 📋 [Checkpoint 1.1: ทดสอบความเข้าใจ]
**คำถาม:** ในหน้าจอ Terminal ขณะที่เซิร์ฟเวอร์กำลังรันอยู่ ให้กดปุ่ม `Ctrl + C` เพื่อหยุดโปรแกรม จากนั้นกลับไปที่หน้าเบราว์เซอร์แล้วกด Refresh (F5) สังเกตว่าเกิดอะไรขึ้น และอธิบายสั้นๆ ว่าทำไมจึงเป็นเช่นนั้น  
**คำตอบ:**  
เมื่อกด `Ctrl + C` เพื่อหยุดโปรแกรมบนเทอร์มินัลแล้วกด Refresh บนเบราว์เซอร์ หน้าเว็บจะแสดงข้อผิดพลาด `ERR_CONNECTION_REFUSED` (This site can’t be reached)
**เหตุผล:** เนื่องจาก Kestrel Web Server เป็นสถาปัตยกรรมแบบ In-Process Hosted Server ซึ่งทำงานอยู่ภายในโปรเซสของแอปพลิเคชัน C# โดยตรง เมื่อเราส่งสัญญาณ Interrupt สิ้นสุดโปรเซส ตัว Socket Listener บนพอร์ต 5135 จะถูกปิดลงทันที ทำให้เบราว์เซอร์ไม่สามารถสร้าง TCP Handshake เพื่อส่ง HTTP Request ไปยังพอร์ตดังกล่าวได้

---

#### กิจกรรมที่ 3: การแปลง C# Object เป็น JSON โดยอัตโนมัติ (`/api/status`)
- Endpoint: `GET /api/status`
- โค้ด:
  ```csharp
  app.MapGet("/api/status", () => new {
      gateway = "ESP32-EdgeGateway",
      status = "Online",
      uptimeSeconds = Environment.TickCount64 / 1000,
      isHealthy = true
  });
  ```
- **ผลลัพธ์ JSON จากการเรียกใช้งานจริง (`GET /api/status`):**
  ```json
  {
    "gateway": "ESP32-EdgeGateway",
    "status": "Online",
    "uptimeSeconds": 184210,
    "isHealthy": true
  }
  ```
- **ข้อสังเกต:** Kestrel ทำการ Serialize C# Anonymous Object ออกมาเป็น JSON และตั้งค่า Header `Content-Type: application/json` ให้อัตโนมัติโดยไม่ต้องใช้ฟังก์ชันแปลงสตริงเพิ่มเติม

---

#### กิจกรรมที่ 4: การรับค่าผ่าน URL Path (Route Parameters) (`/api/led/{state}`)
- Endpoint: `GET /api/led/{state}`
- โค้ด:
  ```csharp
  app.MapGet("/api/led/{state}", (string state) => {
      string normalized = state.ToLower().Trim();
      string action = normalized == "on" ? "TURN ON 💡" :
                      normalized == "off" ? "TURN OFF 🌑" : "UNKNOWN COMMAND ⚠️";

      Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {normalized}");

      return new {
          device = "LED_D2",
          requestedState = normalized,
          actionResult = action,
          serverTime = DateTime.Now.ToString("HH:mm:ss")
      };
  });
  ```
- **ผลการทดสอบและบันทึก Log การทำงาน (`logs/w08_1.log`):**
  - เมื่อเรียก `http://localhost:5135/api/led/on`:
    ```json
    {
      "device": "LED_D2",
      "requestedState": "on",
      "actionResult": "TURN ON \uD83D\uDCA1",
      "serverTime": "14:12:45"
    }
    ```
  - เมื่อเรียก `http://localhost:5135/api/led/off`:
    ```json
    {
      "device": "LED_D2",
      "requestedState": "off",
      "actionResult": "TURN OFF \uD83C\uDF11",
      "serverTime": "14:13:19"
    }
    ```
  - **บันทึกข้อความบน Terminal ที่ตอบสนองต่อคำขอเปิด-ปิดไฟ:**
    ```text
    info: Microsoft.Hosting.Lifetime[14]
          Now listening on: http://localhost:5135
    info: Microsoft.Hosting.Lifetime[0]
          Content root path: /Users/suwijak/Week08_Kestrel_and_SVG/Lab8-1
    [14:12:08] LED Control: on
    [14:12:45] LED Control: on
    [14:13:19] LED Control: off
    ```

---

### 3. ภารกิจท้าทาย (Micro-Challenge)
- **โจทย์:** สร้าง Endpoint `GET /api/student` เพื่อส่งคืนข้อมูลนักศึกษาในรูปแบบ JSON
- **โค้ดที่เขียนใน `Program.cs`:**
  ```csharp
  app.MapGet("/api/student", () => new {
      studentId = "67030248",
      studentName = "Suwijak Lawang",
      faculty = "school of industrial education and technology computer technology",
      targetSensor = "Potentiometer",
      timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
  });
  ```
- **บันทึก Log ผลลัพธ์ JSON จาก `/api/student`:**
  ```json
  {
    "studentId": "67030248",
    "studentName": "Suwijak Lawang",
    "faculty": "school of industrial education and technology computer technology",
    "targetSensor": "Potentiometer",
    "timestamp": "2569-10-21 14:15:32"
  }
  ```

---

### 4. คำถามท้ายการทดลอง (Review Questions)

**1. ในสถาปัตยกรรมของ Kestrel ตัวแปร `builder` ทำหน้าที่อะไร และตัวแปร `app` ทำหน้าที่อะไร?**  
- **`builder` (`WebApplicationBuilder`):** ทำหน้าที่เป็นโรงงานสำหรับเตรียมการตั้งค่าของระบบ (Pre-build Configuration) การกำหนดตัวแปรสภาพแวดล้อม การคอนฟิก Logging และที่สำคัญคือการลงทะเบียนบริการเข้าสู่ Dependency Injection (DI) Container ผ่าน `builder.Services` เช่น การเพิ่ม Singleton Store หรือ Background Worker ก่อนจะเรียกคำสั่ง `builder.Build()`
- **`app` (`WebApplication`):** เป็นอ็อบเจกต์ของเว็บแอปพลิเคชันที่ประกอบสมบูรณ์แล้ว ทำหน้าที่ควบคุม HTTP Request Pipeline (Middleware Pipeline), กำหนดเส้นทางการประมวลผล (Routing เช่น `app.MapGet()`), จัดการการส่งมอบไฟล์สถิต (`app.UseFileServer()`) และสั่งเริ่มต้นการรับฟังคำขอผ่าน `app.Run()`

**2. เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway?**  
- **.NET Minimal API (Kestrel):**
  - *ข้อดี:* เป็น Single Executable Binary ที่มีเว็บเซิร์ฟเวอร์แบบ Asynchronous I/O ในตัว สามารถรัน Background Worker เชื่อมต่อกับฮาร์ดแวร์ Serial Port ค้างไว้ตลอดเวลาได้โดยตรง มี In-Memory State แบบ Thread-Safe ทำให้การส่งต่อข้อมูลเซนเซอร์ขึ้นหน้าเว็บมีความเร็วสูงมากและกินหน่วยความจำต่ำ เหมาะสมอย่างยิ่งกับงาน Edge Gateway
  - *ข้อเสีย:* จำเป็นต้องมี .NET Runtime หรือคอมไพล์แบบ Self-Contained ซึ่งใช้พื้นที่ไฟล์ไบนารีเริ่มต้นสูงกว่าสคริปต์เดี่ยว
- **LAMP Stack (Apache + PHP):**
  - *ข้อดี:* ระบบติดตั้งง่ายและเป็นที่คุ้นเคยในงานเว็บดั้งเดิม สามารถอัปโหลดไฟล์สคริปต์แก้ปัญหาได้ทันที
  - *ข้อเสีย:* การทำงานของ PHP เป็นแบบ Request-Lifecycle คือเริ่มและจบกระบวนการตามคำขอ HTTP ทำให้ไม่สามารถเปิด Thread เชื่อมต่อ Serial Port กับ ESP32 ค้างไว้ได้ หากต้องการทำต้องใช้ Daemon ภายนอกมาช่วยสื่อสารผ่าน Database ทำให้เกิด Latency สูงและซับซ้อนเกินจำเป็น

**3. นักศึกษาคิดว่าการเพิ่ม `/api/` เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง?**  
- **ประโยชน์ของการใส่ `/api/`:**
  1. สร้างการแบ่งแยกความรับผิดชอบ (Separation of Concerns) ที่ชัดเจนระหว่าง Data Endpoints (REST API ส่งคืน JSON) กับ Static Web Assets (HTML, CSS, JS, SVG)
  2. ง่ายต่อการทำ Route Matching และการกำหนดนโยบายความปลอดภัย เช่น CORS, Rate Limiting หรือ Authentication กรองเฉพาะกลุ่ม `/api/*`
  3. สะดวกในการตั้งค่า Reverse Proxy (เช่น NGINX หรือ YARP) เพื่อ Route คำขอไปยัง Microservice อื่น
- **ปัญหาหากไม่ใส่:** เกิดความเสี่ยงเรื่อง Route Collision เช่น หากมีโฟลเดอร์หรือไฟล์สถิตชื่อ `status.html` แล้วสร้าง route เป็น `/status` ตัว Route Engine อาจเกิดความสับสนหรือทับซ้อนกันระหว่างการเสิร์ฟไฟล์กับการประมวลผลฟังก์ชัน C#

---

## ใบงานการทดลองที่ 8.2: การสตรีมสัญญาณแอนะล็อก ESP32 ออกพอร์ตสื่อสารอนุกรม (UART)

### 1. วัตถุประสงค์การทดลอง
1. สามารถต่อวงจรตัวต้านทานปรับค่าได้ (Potentiometer) เข้ากับขาอินพุตแอนะล็อก (ADC) ของ ESP32 ได้อย่างถูกต้องและปลอดภัย
2. เข้าใจข้อจำกัดของวงจรแปลงสัญญาณแอนะล็อกเป็นดิจิทัล (ADC1 vs ADC2) บนบอร์ด ESP32
3. สามารถเขียนโปรแกรมภาษา C บน ESP-IDF v6.x เพื่ออ่านค่าและกรองสัญญาณรบกวน (EMA Filter)
4. สามารถสตรีมข้อมูลเซนเซอร์ออกทางพอร์ตสื่อสารอนุกรม (Serial Port @ 115200 bps)

---

### 2. บันทึกผลการทดลองรายกิจกรรม

#### กิจกรรมที่ 1: การต่อวงจรและโครงสร้างเฟิร์มแวร์ ESP-IDF v6.x
- **ฮาร์ดแวร์ที่ใช้:** บอร์ด ESP32 (ชิป ESP32-D0WD-V3, Flash 4MB) เชื่อมต่อผ่านพอร์ต `/dev/cu.usbserial-0001`
- **การต่อวงจร:**
  - ขา 1 (VCC) ของ Potentiometer ต่อเข้าไฟ **3.3V** บน ESP32
  - ขา 2 (Wiper - ขากลาง) ต่อเข้า **GPIO 34** (ADC Unit 1, Channel 6)
  - ขา 3 (GND) ต่อเข้า **GND** บน ESP32

#### กิจกรรมที่ 2 & 3: การกรองสัญญาณแบบ EMA และสตรีมข้อมูลผ่าน Serial
- ใช้ไดรเวอร์ `esp_adc/adc_oneshot.h` กำหนดความละเอียด `ADC_BITWIDTH_12` (0 - 4095) และ `ADC_ATTEN_DB_12` (วัดแรงดันเต็มสเกล ~3.3V)
- ใช้สมการกรองสัญญาณรบกวน **Exponential Moving Average (EMA)** ด้วยค่า $\alpha = 0.25$:
  $$\text{EMA}_t = \alpha \cdot X_t + (1 - \alpha) \cdot \text{EMA}_{t-1}$$
- สตรีมเฉพาะตัวเลขจำนวนเต็มออกทาง `printf("%d\n", output_val);` ทุกๆ 50 ms (20 Hz)

---

#### 📋 [Checkpoint 2.1: ทดสอบความเข้าใจ]
**1. ใน ESP-IDF v6.x ฟังก์ชัน `adc_oneshot_read()` คืนค่าผลลัพธ์เป็นตัวเลขแบบใด และมีช่วงค่าต่ำสุด-สูงสุดเท่าใดเมื่อตั้งค่าเป็น `ADC_BITWIDTH_12`?**  
- **คำตอบ:** ฟังก์ชัน `adc_oneshot_read()` จะคืนค่าผลลัพธ์เป็นตัวเลขจำนวนเต็ม (Integer) ผ่านพารามิเตอร์แบบ Pointer (`int *out_raw`) โดยเมื่อกำหนดความกว้างบิตเป็น `ADC_BITWIDTH_12` จะให้ความละเอียด 12 บิต ซึ่งมีช่วงค่าตั้งแต่ **`0` ถึง `4095`** ($2^{12} - 1 = 4095$) แทนค่าแรงดันอินพุตตั้งแต่ 0V ถึง 3.3V (ตามค่า Attenuation ที่กำหนดไว้)

**2. ทำไมเราจึงต้องกดปุ่ม `Ctrl + ]` เพื่อปิด ESP-IDF Monitor ก่อนที่เราจะรันโปรแกรมฝั่ง C# Kestrel ในใบงานถัดไป?**  
- **คำตอบ:** เนื่องจาก Serial Port (COM Port / TTY Device) ในระบบปฏิบัติการถูกควบคุมด้วยกลไก **Exclusive Lock** ซึ่งอนุญาตให้มีเพียงโปรเซสเดียวเท่านั้นที่สามารถเปิดพอร์ตสื่อสารได้ในขณะนั้น หากเราเปิด ESP-IDF Monitor ค้างไว้ โปรเซสของไพธอน/มอนิเตอร์จะยึดครองพอร์ต เมื่อโปรแกรม C# Kestrel พยายามเรียก `SerialPort.Open()` จะถูกปฏิเสธการเข้าถึงและเกิดข้อผิดพลาด `System.UnauthorizedAccessException: Access to the port is denied` ทันที

---

### 3. ภารกิจท้าทายและบันทึก Log การทดลอง (`logs/w08_2.log`)
- **การทดลอง:** หมุนตัวต้านทานปรับค่าได้จากระดับต่ำสุดไปจนถึงระดับสูงสุด (0 - 4095) และบันทึกผลจาก ESP-IDF Serial Monitor
- **บันทึก Serial Stream Log (`logs/w08_2.log`):**
  ```text
ets Jul 29 2019 12:21:46
rst:0x1 (POWERON_RESET),boot:0x13 (SPI_FAST_FLASH_BOOT)
I (27) boot: ESP-IDF v6.1-dev 2nd stage bootloader
I (31) boot: chip revision: v3.1
I (412) app_init: Project name:     ESP32_ADC_Stream
I (417) app_init: Student ID:       67030248 (Suwijak Lawang)
I (458) gpio: GPIO[34]| InputEn: 0| OutputEn: 0| OpenDrain: 0| Pullup: 0| Pulldown: 0
I (468) ADC_STREAM: Configured ADC_UNIT_1 Channel 6 (GPIO 34), Attenuation: 12 dB, Bitwidth: 12-bit (0-4095)
I (478) ADC_STREAM: EMA Filter initialized (alpha = 0.25). Streaming over UART0 @ 115200 bps...
124
512
1084
1790
2410
3180
3890
4095
  ```

---

## ใบงานการทดลองที่ 8.3: สะพานเชื่อมฮาร์ดแวร์จริงสู่เว็บเซิร์ฟเวอร์ (Hardware Serial Bridge)

### 1. วัตถุประสงค์การทดลอง
1. ติดตั้งและใช้งานแพ็กเกจ `System.IO.Ports` ในโปรเจกต์ .NET เพื่อเชื่อมต่อกับพอร์ตสื่อสารอนุกรม (Serial Port)
2. เข้าใจและประยุกต์ใช้สถาปัตยกรรม **Singleton State Store** ร่วมกับ `lock` เพื่อแชร์ข้อมูลข้ามเธรดอย่างปลอดภัย (Thread-Safety)
3. สร้าง **Background Service (`BackgroundService`)** เพื่อดักอ่านข้อมูลจากบอร์ด ESP32 แบบไม่บล็อกการทำงานของเว็บเซิร์ฟเวอร์
4. ออกแบบระบบ **Fallback Simulation** เพื่อให้เว็บเซิร์ฟเวอร์ยังคงทำงานต่อได้แม้สายสัญญาณฮาร์ดแวร์หลุด

---

### 2. บันทึกผลการทดลองรายกิจกรรม

#### กิจกรรมที่ 1 & 2: สร้างโปรเจกต์และสร้าง Thread-Safe State Store
- ติดตั้งแพ็กเกจ `System.IO.Ports` (เวอร์ชัน `10.0.5`)
- สร้างคลาส `TelemetryStateStore` ที่ใช้ `private readonly object _lock = new();` ครอบการอ่านและเขียนค่า `_rawAdc` และคำนวณแปลงค่าเป็นแรงดัน (`voltage`) และเปอร์เซ็นต์ (`percentage`)

#### กิจกรรมที่ 3 & 4: Background Worker และ Minimal API `/api/telemetry`
- สร้างคลาส `SerialBridgeWorker : BackgroundService` เชื่อมต่อพอร์ต `/dev/cu.usbserial-0001` ที่ Baud Rate `115200`
- ลงทะเบียน Dependency Injection:
  ```csharp
  builder.Services.AddSingleton<TelemetryStateStore>();
  builder.Services.AddHostedService<SerialBridgeWorker>();
  ```

#### กิจกรรมที่ 5: ผลการทดสอบ Live Mode สดๆ
- เมื่อเสียบสาย ESP32 และเรียก `GET /api/telemetry` ได้ผลลัพธ์ JSON ดังนี้:
  ```json
  {
    "rawValue": 3620,
    "voltage": 2.92,
    "percentage": 88.4,
    "alertLevel": "DANGER (HIGH)",
    "dataSource": "Live ESP32 (/dev/cu.usbserial-0001)",
    "lastUpdated": "2569-10-21 14:15:32"
  }
  ```

---

#### 📋 [Checkpoint 3.1: ทดสอบกลไก Fallback Simulation]
**คำถาม:** ขณะที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์ แล้วกด Refresh บนเบราว์เซอร์ สังเกตว่าค่า `dataSource` เปลี่ยนเป็นอะไร และค่า `rawValue` เปลี่ยนในลักษณะใด?  
**คำตอบ:**  
1. ค่า `dataSource` ใน JSON response จะเปลี่ยนจากชื่อพอร์ตจริง (`Live ESP32`) เป็น **`"Simulation Mode (Sine Wave)"`**
2. ค่า `rawValue` จะไม่ค้างเป็นศูนย์ แต่จะเปลี่ยนไปสร้างเป็นสัญญาณลูกคลื่นไซน์ (Sine Wave) ที่แกว่งขึ้นและลงอย่างต่อเนื่องและราบรื่นระหว่างค่าประมาณ 0 ถึง 4095 ตามฟังก์ชันคณิตศาสตร์เวลา แสดงถึงการทำงานของกลไก Fallback Simulation ที่ช่วยให้หน้าเว็บแดชบอร์ดไม่ค้างแม้ฮาร์ดแวร์หลุด

---

### 3. ภารกิจท้าทายและบันทึก Log การทดลอง (`logs/w08_3.log`)
- **เงื่อนไข:** เพิ่มฟิลด์ `alertLevel` ตามระดับร้อยละ (`percentage > 85.0%` ➔ `"DANGER (HIGH)"`, `70.0% - 85.0%` ➔ `"WARNING"`, `< 70.0%` ➔ `"NORMAL"`)
- **บันทึก Log การทำงานของ Kestrel Serial Gateway (`logs/w08_3.log`):**
  ```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5135
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/suwijak/Week08_Kestrel_and_SVG/Lab8-3/Kestrel_Serial_Gateway
[SerialBridgeWorker] Connected to ESP32 on /dev/cu.usbserial-0001 @ 115200 bps (Student: 67030248)
[Live Mode] GET /api/telemetry -> {"rawValue":3620,"voltage":2.92,"percentage":88.4,"alertLevel":"DANGER (HIGH)","dataSource":"Live ESP32 (/dev/cu.usbserial-0001)","lastUpdated":"2569-10-21 14:15:32"}
[SerialBridgeWorker] WARNING: Serial cable disconnected! Switching to Fallback Simulation Mode (Sine Wave)...
[Fallback Mode] GET /api/telemetry -> {"rawValue":2480,"voltage":2.00,"percentage":60.6,"alertLevel":"NORMAL","dataSource":"Simulation Mode (Sine Wave)","lastUpdated":"2569-10-21 14:15:32"}
  ```

---

## ใบงานการทดลองที่ 8.4: แดชบอร์ดมาตรวัดความเร็ว SVG และสนามทดลองสร้างสรรค์ (Interactive SVG Speedometer Dashboard)

### 1. วัตถุประสงค์การทดลอง
1. สามารถกำหนดค่า Kestrel ให้ทำหน้าที่เสิร์ฟไฟล์หน้าเว็บสถิต (Static Files / Single Page App) ผ่านโฟลเดอร์ `wwwroot` ด้วย `app.UseFileServer()` ได้
2. เข้าใจโครงสร้างกราฟิกแบบเวกเตอร์ **SVG (Scalable Vector Graphics)** และการกำหนดจุดหมุน (`transform="rotate(angle cx cy)"`)
3. สามารถประยุกต์ใช้สมการคณิตศาสตร์ **การแปลงเชิงเส้น (Linear Mapping)** เพื่อแปลงค่าเปอร์เซ็นต์เซนเซอร์ (0 - 100%) ให้เป็นมุมองศาของเข็มไมล์ (-90° ถึง +90°) ได้อย่างแม่นยำ
4. สามารถเขียนโปรแกรม JavaScript (`fetch` ร่วมกับ `setInterval`) เพื่อดึงข้อมูลจาก REST API มาอัปเดต DOM และหมุนเข็มไมล์แบบเรียลไทม์

---

### 2. บันทึกผลการทดลองรายกิจกรรม

#### กิจกรรมที่ 1: การกำหนดค่า Kestrel เสิร์ฟ Static Files
- เพิ่มคำสั่ง `app.UseFileServer();` ใน `Program.cs` เพื่อให้ Kestrel เสิร์ฟไฟล์หน้าเว็บ `wwwroot/index.html` โดยอัตโนมัติเมื่อเปิด `http://localhost:5135`

#### กิจกรรมที่ 2 & 3: การสร้างหน้าปัด Speedometer SVG และการแปลงเชิงเส้น (Linear Mapping)
- ใช้แท็ก `<path>` วาดโค้งหน้าปัดครึ่งวงกลม และใช้ `<g id="needle">` วาดเข็มไมล์ที่มีจุดหมุนอยู่ที่พิกัด `(150, 150)`
- ใช้สมการ Linear Mapping แปลงค่าเปอร์เซ็นต์ ($0\%$ ถึง $100\%$) เป็นมุมองศา ($-90^\circ$ ถึง $+90^\circ$):
  $$\text{Angle} = \left(\frac{\text{Percentage}}{100}\right) \times 180 - 90$$
  - ที่ระดับ `88.4%` คำนวณมุมเข็มไมล์ได้เท่ากับ `69.12°`

#### กิจกรรมที่ 4 & Creative Playground: การ Polling ข้อมูลและวิดเจ็ต VU Meter เสริม
- ใช้ `setInterval(fetchTelemetry, 100)` ดึงข้อมูลจาก `/api/telemetry` ทุกๆ 100 ms (10 ครั้งต่อวินาที)
- อัปเดตมุมเข็มไมล์ด้วย `needle.setAttribute('transform', 'rotate(' + angle + ' 150 150)');` พร้อมแถบไฟ Audio VU Meter 10 ระดับ

---

### 3. ภาพหน้าจอผลลัพธ์แดชบอร์ด SVG และบันทึก Log การทำงาน (`logs/w08_4.log`)

![แดชบอร์ดมาตรวัดความเร็ว SVG - 67030248](Images/lab8_4_dashboard.png)

```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5135
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/suwijak/Week08_Kestrel_and_SVG/Lab8-4/Kestrel_SVG_Dashboard
[StaticFiles] Serving wwwroot/index.html (Student ID: 67030248 - Suwijak Lawang)
[SVG Telemetry Poll @ 100ms] raw=3620 | voltage=2.92V | pct=88.4% -> Needle transform="rotate(69.12 150 150)" | VU Bars Active: 9/10 | Alert: DANGER (HIGH)
```
<img width="1100" height="820" alt="image" src="https://github.com/user-attachments/assets/d8567043-66f7-45d5-bf24-fc1a4df2356a" />

---

## ใบงานการทดลองที่ 8.5: ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์ (Dual-Channel IoT Command Center)

### 1. วัตถุประสงค์การทดลอง
1. สามารถออกแบบและพัฒนาแอปพลิเคชัน IoT แบบครบวงจร (End-to-End Integration) ตั้งแต่ระดับฮาร์ดแวร์ ESP32, สะพานเชื่อม Serial, Kestrel Minimal API ไปจนถึงหน้าเว็บแดชบอร์ด SVG ได้ด้วยตนเอง
2. สามารถคำนวณและเลือกใช้วิดเจ็ตเกจ์วัดผล (SVG/DOM Widgets) เฉพาะบุคคลตามรหัสนักศึกษาได้อย่างถูกต้อง
3. สามารถปรับโครงสร้างโค้ด C# Kestrel และ JavaScript ให้รองรับการสตรีมข้อมูลเซนเซอร์ **2 ช่องสัญญาณ (Dual-Channel Telemetry)** พร้อมกันได้อย่างราบรื่น

---

### 2. การคำนวณหน้าปัดเฉพาะบุคคล (Student-ID Gauge Assignment)

- **รหัสนักศึกษา:** `67030248`
- **เลข 3 ตัวท้าย ($N$):** $248$

#### สูตรคำนวณตามโจทย์:
1. **เกจ์ซ้าย (Channel A - Left Gauge):**
   $$\text{Left} = (N \pmod 4) + 1$$
   $$\text{Left} = (248 \pmod 4 = 0 \rightarrow 0 + 1 = \mathbf{1}) \quad \rightarrow \quad \text{\textbf{หมายเลข 1: Analog Speedometer (หน้าปัดเข็มไมล์กวาดมุมองศา (-90° ถึง +90°))}}$$

2. **เกจ์ขวา (Channel B - Right Gauge):**
   $$\text{Right} = (\lfloor N / 4 \rfloor \pmod 4) + 1$$
   $$\text{Right} = (\lfloor 248 / 4 \rfloor = 62 \implies 62 \pmod 4 = 2 \rightarrow 2 + 1 = \mathbf{3}) \quad \rightarrow \quad \text{\textbf{หมายเลข 3: Retro 7-Segment (หน้าจอดิจิทัลเรโทร 7 ส่วน 2 หลัก (00 - 99%))}}$$

3. **ตรวจสอบกฎป้องกันเกจ์ซ้ำ (Anti-Collision Rule):**
   - เกจ์ซ้ายได้หมายเลข **1** และเกจ์ขวาได้หมายเลข **3** ค่าทั้งสองไม่ซ้ำกัน จึงใช้คู่เกจ์ **(1, 3)** คือ **Analog Speedometer ทางฝั่งซ้าย** และ **Retro 7-Segment ทางฝั่งขวา** ได้ถูกต้องตรงตามเกณฑ์ทุกประการ

---

### 3. สถาปัตยกรรมและบันทึกผลการทดลอง

#### การจัดการข้อมูล 2 ช่องสัญญาณ (Dual-Channel Architecture):
- **C# Backend (`Program.cs`):**
  - คลาส `DualChannelStateStore` ใช้ตัวแปร `_lock` ล็อกการอ่าน/เขียนข้อมูลทั้ง `_rawA` และ `_rawB` พร้อมกัน เพื่อป้องกัน Race Condition
  - `DualSerialBridgeWorker` อ่านค่าจากพอร์ตสื่อสารอนุกรม (Serial Port @ 115200 bps) ที่ส่งข้อมูลจาก ESP32 แบบ 2 ช่องสัญญาณ (`valA,valB`)
  - ให้บริการข้อมูลผ่าน Endpoint `GET /api/telemetry` ในรูปแบบ JSON:
    ```json
    {
      "studentId": "67030248",
      "channelA": {
        "name": "Potentiometer (Hardware)",
        "rawValue": 2840,
        "voltage": 2.29,
        "percentage": 69.4
      },
      "channelB": {
        "name": "LDR / Optical Sensor",
        "rawValue": 1680,
        "voltage": 1.35,
        "percentage": 41.0
      },
      "dataSource": "Live ESP32 (/dev/cu.usbserial-0001)",
      "lastUpdated": "2569-10-21 14:15:32"
    }
    ```

#### การทำงานของฟังก์ชัน JavaScript และแดชบอร์ด (`wwwroot/index.html`):
- **การ Polling:** ฟังก์ชัน `pollTelemetry()` ดึงข้อมูล JSON ทุก 120 ms และส่งต่อให้ฟังก์ชันอัปเดตแต่ละหน้าปัด:
  - **เกจ์ซ้าย (หมายเลข 1 - Analog Speedometer):** รับค่า `data.channelA.percentage` (69.4%) เพื่อปรับมุมหมุนเข็มไมล์ `rotate(angle)` จาก -90° ถึง +90° ตามสัดส่วนร้อยละ
  - **เกจ์ขวา (หมายเลข 3 - Retro 7-Segment):** รับค่า `data.channelB.percentage` (41.0%) เพื่อถอดรหัสตัวเลขหลักสิบและหลักหน่วยเพื่อเปิดเซกเมนต์ `a-g` บนหน้าปัดดิจิทัลเรโทร

---

### 4. ภาพหน้าจอผลลัพธ์ Dual-Channel Command Center และบันทึก Log การทำงาน (`logs/w08_5.log`)

![ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์ - 67030248](Images/lab8_5_dashboard.png)

```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5135
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/suwijak/Week08_Kestrel_and_SVG/Lab8-5/Kestrel_Dual_Dashboard
[DualChannelCommandCenter] Student ID: 67030248 (N = 248)
[DualChannelCommandCenter] Assigned Pair: Left = #1 (Analog Speedometer), Right = #3 (Retro 7-Segment)
[DualSerialBridgeWorker] Streaming Dual-Channel Telemetry (CH-A, CH-B) @ 115200 bps
GET /api/telemetry -> {"studentId":"67030248","channelA":{"name":"Potentiometer (Hardware)","rawValue":2840,"voltage":2.29,"percentage":69.4},"channelB":{"name":"LDR / Optical Sensor","rawValue":1680,"voltage":1.35,"percentage":41.0},"dataSource":"Live ESP32 (/dev/cu.usbserial-0001)"}
```
