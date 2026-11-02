# สรุปเนื้อหาและคู่มือเตรียมสอบวิชา IoT: Week 08
## สถาปัตยกรรม Edge Web Server, Kestrel Minimal API และ Interactive SVG IoT Dashboard

---

## 📑 สารบัญเนื้อหา (Table of Contents)
1. [ภาพรวมโครงสร้างบทเรียนและโครงสร้างไฟล์](#1-ภาพรวมโครงสร้างบทเรียนและโครงสร้างไฟล์)
2. [บทที่ 1: สถาปัตยกรรม Edge Web Server และ Kestrel Engine](#บทที่-1-สถาปัตยกรรม-edge-web-server-และ-kestrel-engine)
3. [บทที่ 2: โพรโทคอล HTTP และ Modern C# Minimal APIs](#บทที่-2-โพรโทคอล-http-และ-modern-c-minimal-apis)
4. [บทที่ 3: สะพานเชื่อมฮาร์ดแวร์และการจัดการ Concurrency](#บทที่-3-สะพานเชื่อมฮาร์ดแวร์และการจัดการ-concurrency)
5. [บทที่ 4: การแสดงผลเชิงภาพด้วย SVG และแผงควบคุมเรียลไทม์](#บทที่-4-การแสดงผลเชิงภาพด้วย-svg-และแผงควบคุมเรียลไทม์)
6. [สรุปสาระสำคัญเชิงปฏิบัติการ (Lab 8.1 - 8.5)](#สรุปสาระสำคัญเชิงปฏิบัติการ-lab-81---85)
7. [สูตรคำนวณและคณิตศาสตร์ที่มักออกสอบ](#สูตรคำนวณและคณิตศาสตร์ที่มักออกสอบ)
8. [รวมประเด็นหลอกยอดฮิต & ข้อควรระวังในข้อสอบ (Trap & Pitfalls)](#รวมประเด็นหลอกยอดฮิต--ข้อควรระวังในข้อสอบ-trap--pitfalls)
9. [แนวข้อสอบทฤษฎีและปฏิบัติ (Exam Cheat Sheet & Q&A)](#แนวข้อสอบทฤษฎีและปฏิบัติ-exam-cheat-sheet--qa)
10. [อภิธานศัพท์สำคัญ (Glossary)](#อภิธานศัพท์สำคัญ-glossary)

---

## 1. ภาพรวมโครงสร้างบทเรียนและโครงสร้างไฟล์

### โครงสร้างไฟล์ในโฟลเดอร์ Week-08-Kestrel-Web-Server
* **เอกสารบทเรียนทฤษฎี (Theory Notes):**
  * `01-Edge-WebServer-and-Kestrel-Architecture.md`: เปรียบเทียบ LAMP Stack กับ Kestrel, The C10K Problem, Non-blocking I/O และ System.IO.Pipelines
  * `02-HTTP-Protocols-and-Minimal-APIs.md`: วงจร HTTP Request-Response, ข้อดีของ Minimal APIs (.NET 8+), การแมป Path/Query Parameters และ Auto JSON Serialization
  * `03-Hardware-to-Server-Bridge-and-Concurrency.md`: การเชื่อมต่อ UART USB ด้วย System.IO.Ports, Producer-Consumer Pattern, BackgroundService, Thread-Safety (lock) และ Fallback Simulation
  * `04-SVG-Visualization-and-Realtime-Dashboards.md`: เหตุผลที่ใช้ SVG บนงาน Edge IoT, Linear Mapping สูตรแปลงค่า ADC สู่มุมองศา, stroke-dasharray/dashoffset และ Client-side Polling
  * `05-Glossary.md`: ตารางคำศัพท์และคำนิยามสำคัญ 20+ คำ
* **ใบงานภาคปฏิบัติ (Labsheets):**
  * `06-Labsheet-08-1`: เริ่มต้นสร้างเว็บด้วย Minimal API คืนค่าข้อความและ JSON
  * `07-Labsheet-08-2`: ต่อวงจร ESP32 อ่านค่า Potentiometer (ADC1 GPIO34) ทำ Moving Average และสตรีม Serial UART 115200 bps
  * `08-Labsheet-08-3`: รวมร่าง ESP32 + Kestrel Web Server ดักอ่าน Serial ด้วย BackgroundService เก็บลง Thread-Safe Store
  * `09-Labsheet-08-4`: สร้างเว็บแสดงผล SVG Speedometer Gauge เคลื่อนไหวลื่นไหลด้วย CSS Transition
  * `10-Labsheet-08-5`: Command Center เซนเซอร์ 2 ช่องสัญญาณ (Dual-Channel) พร้อมสูตรคำนวณวิดเจ็ตตามรหัสนักศึกษา (Speedometer, VU Meter, 7-Segment, Liquid Tank)
* **โฟลเดอร์โค้ดและทรัพยากร:**
  * `MyLab/`: โฟลเดอร์งานปฏิบัติการของนักศึกษา (Lab8-1 ถึง Lab8-5)
  * `References/`: เอกสารอ้างอิงเชิงลึกจาก Microsoft (Kestrel, Minimal APIs, Controller-based APIs)
  * `Images/`: ภาพจำลองวิดเจ็ตต่างๆ

---

## บทที่ 1: สถาปัตยกรรม Edge Web Server และ Kestrel Engine

### 1.1 การเปรียบเทียบ: LAMP Stack vs Modern Kestrel In-Process Web Server
ในระบบ Embedded Edge IoT (เช่น บน Raspberry Pi หรือ Industrial Box PC) ทรัพยากรฮาร์ดแวร์มีจำกัด

| มิติการเปรียบเทียบ | สถาปัตยกรรมเดิม (LAMP Stack) | สถาปัตยกรรมใหม่ (.NET Kestrel Gateway) |
| :--- | :--- | :--- |
| **โครงสร้างระบบ** | Process แยกส่วน (Apache + PHP-FPM + MySQL) ต้องคอนฟิกหลายจุด | Single OS Executable (ไบนารีเดียว มี Web Server ฝังในตัว) |
| **I/O Model** | Multi-Process / Multi-Thread (Synchronous Blocking) | Asynchronous Non-blocking I/O (epoll / IOCP / Pipelines) |
| **การใช้ RAM ต่อ Request** | สูงมาก (~25 MB ต่อ Worker Process) | ต่ำมาก (~2-4 KB สำหรับ Socket Buffer) |
| **การเข้าถึงฮาร์ดแวร์ (I/O)** | ทำได้ยาก ติด Sandbox สิทธิ์เข้าถึง Serial Port ซับซ้อน | ทำได้โดยตรงและรวดเร็ว อยู่ใน Application Process เดียวกัน |
| **ปัญหาคอขวด (Bottleneck)** | Thread Exhaustion และ Context Switching พุ่งสูงเมื่อโหลดสูง | ทำงานแบบ Event Loop กระจายงานผ่าน ThreadPool ไม่บล็อก CPU |

### 1.2 ปัญหาเชิงประจักษ์ในสภาวะโหลดสูง (The C10K Problem)
1. **Thread / Memory Exhaustion:**
   * LAMP: ถ้ามี ESP32 จำนวน 1,000 บอร์ด ส่ง HTTP POST เข้ามาทุกวินาที แล้วเขียน DB ใช้เวลา 200 ms จะเกิด 200 Concurrent Processes ใช้ RAM = $200 \times 25\text{ MB} = 5,000\text{ MB} \approx 5\text{ GB}$ (ถ้า 5,000 บอร์ดใช้ถึง 25 GB เกิด HTTP 503 หรือ Linux OOM Killer ปิดโปรเซสทิ้ง)
   * Kestrel: อาศัย Non-blocking I/O ใช้ RAM Socket Buffer เพียง $\approx 4\text{ MB}$ รวมกับ Base Memory ของ .NET Runtime แล้วกิน RAM ทั้งหมด **ไม่ถึง 100 MB**
2. **Context Switching Overhead:**
   * เมื่อรัน Thread จำนวนมหาศาล สลับไปมาบน CPU Core ที่มีจำกัด Kernel CPU Time (`%sys`) จะพุ่งสูงเกิน 60% สูญเสียพลังประมวลผลไปกับการย้าย Stack/Register
   * Kestrel ใช้ Thread Pool ขนาดเล็ก (เท่าจำนวน Core หรือ 8-16 Threads) ทำงานแบบ Async Event Loop ทำให้ `%user` ถูกใช้ประมวลผลจริงมากกว่า 95%
3. **Blocking I/O vs Asynchronous Non-blocking (`await`):**
   * Blocking (LAMP): Thread ที่คอยรับคำขอต้องหยุดรอ (Lock) จนกว่าฐานข้อมูลหรือฮาร์ดแวร์จะตอบกลับ
   * Non-blocking (Kestrel): เมื่อสั่งงาน I/O ระบบจะ Yield Thread คืนให้ Thread Pool ไปรับงาน Client ตัวอื่นทันที เมื่อ I/O เสร็จ ระบบปฏิบัติการจะส่งสัญญาณมาปลุกให้กลับมาประมวลผลต่อ

### 1.3 กลไก System.IO.Pipelines และ Zero-Allocation Parsing
* Kestrel ออกแบบมาเพื่อลดภาระของ **Garbage Collector (GC)**
* ใช้เทคนิค **Zero-Allocation**: ไม่แปลง Byte ดิบของ HTTP Header เป็น `string` บน Heap หากไม่จำเป็น แต่ใช้ Pointer หรือ `Span<T>` / `Memory<T>` ชี้ไปยัง Memory Buffer ก้อนเดิมโดยตรง ทำให้ประหยัด RAM และทำงานได้เร็วระดับหัวแถวของโลก (TechEmpower Benchmark)

---

## บทที่ 2: โพรโทคอล HTTP และ Modern C# Minimal APIs

### 2.1 วงจร HTTP Request-Response Lifecycle
1. **TCP 3-Way Handshake:** ไคลเอนต์เปิดพอร์ตเชื่อมต่อกับเซิร์ฟเวอร์ (SYN $\rightarrow$ SYN-ACK $\rightarrow$ ACK)
2. **HTTP Request:** ไคลเอนต์ส่ง Request Line + Headers + Body
3. **Routing & Handling:** Kestrel Routing Engine ตรวจสอบ Method และ URL Path แล้วส่งต่อไปยัง Endpoint Handler
4. **Processing & Serialization:** ประมวลผลตรรกะ และแปลงผลลัพธ์เป็นข้อความตาม Header `Content-Type`
5. **HTTP Response:** ส่ง Status Code (เช่น 200 OK) + Response Headers + Payload กลับไปยังไคลเอนต์

### 2.2 โครงสร้างคำสั่ง Minimal APIs ใน Modern C# (.NET 8+)
Minimal API ตัด Controller, Base Class, และ Attribute รกๆ ออกทั้งหมด ทำให้โค้ดสั้นและมีประสิทธิภาพสูงสุด:

```csharp
var builder = WebApplication.CreateBuilder(args);

// กำหนดการรับคำขอจากทุก IP ภายนอก (LAN Binding)
builder.WebHost.ConfigureKestrel(options => {
    options.ListenAnyIP(5000);
});

// กำหนดนโยบาย CORS เพื่อให้หน้าเว็บจากต้นทางอื่นเรียก API ได้
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles(); // ให้บริการไฟล์ HTML/CSS ใน wwwroot

// 1. Endpoint คืนข้อความธรรมดา
app.MapGet("/api/ping", () => "pong");

// 2. Zero-Config Automatic JSON Serialization (Anonymous Object)
app.MapGet("/api/status", () => new {
    device = "ESP32-Gateway",
    isHealthy = true,
    uptimeSec = Environment.TickCount64 / 1000
});

// 3. Route Parameter: รับค่าผ่าน URL Path
app.MapGet("/api/relay/{channel}/{state}", (int channel, string state) => {
    return Results.Ok(new { relay = channel, action = state });
});

// 4. Query Parameter: รับค่าต่อท้ายเครื่องหมาย ? เช่น /api/fan?speed=80
app.MapGet("/api/fan", (int speed) => {
    int safeSpeed = Math.Clamp(speed, 0, 100);
    return Results.Ok(new { fanSpeed = safeSpeed });
});

app.Run();
```

### 2.3 Data Contract และข้อดีของ C# `record`
* การใช้ **`public record SensorReading(...)`** ช่วยสร้าง Immutable Type ที่มีความปลอดภัยสูง และมีขนาดกะทัดรัด
* Kestrel จะเรียก `System.Text.Json` เพื่อแปลง C# Record ให้กลายเป็น JSON พร้อมแนบ Header `Content-Type: application/json; charset=utf-8` ให้อัตโนมัติ

---

## บทที่ 3: สะพานเชื่อมฮาร์ดแวร์และการจัดการ Concurrency

### 3.1 ข้อผิดพลาดร้ายแรง: การเปิด-ปิด Serial Port ใน HTTP Request
**คำเตือนเด็ดขาด:** ห้ามสั่ง `new SerialPort().Open()` ข้างในฟังก์ชัน Endpoint ของ Web API เด็ดขาด เพราะ:
1. **Port Latency:** การเปิดพอร์ต Virtual COM ใช้เวลา 10-50 ms ทำให้เว็บช้าและหน่วง
2. **Resource Locking (`UnauthorizedAccessException`):** พอร์ต COM ถูกเปิดได้ครั้งละ 1 Process/Thread เท่านั้น หากมี 2 Request เข้ามาพร้อมกัน อีกคนจะพังทันที
3. **Buffer Loss:** ข้อมูลที่ ESP32 ยิงมาต่อเนื่องจะสูญหาย หากไม่ได้เปิดพอร์ตดักรอไว้

### 3.2 รูปแบบสถาปัตยกรรม Producer-Consumer
* **Producer (ผู้ผลิต):** `BackgroundService` หรือ Worker Thread ทำหน้าที่เปิดพอร์ต Serial Port ทิ้งไว้ครั้งเดียว แล้ววนลูปอ่านค่า (`ReadLine()`) จาก ESP32 ตลอดเวลา
* **Shared Store (คลังข้อมูลกลาง):** `TelemetryStateStore` ทำหน้าที่เก็บค่าล่าสุดในหน่วยความจำ
* **Consumer (ผู้บริโภค):** Minimal API (เช่น `GET /api/telemetry`) เข้ามาหยิบค่าจาก State Store ส่งให้ Web Browser ทันทีในระดับ Sub-millisecond

### 3.3 การจัดการ Thread-Safety ด้วยคีย์เวิร์ด `lock`
ป้องกันปัญหา **Race Condition** หรือ **Torn Read** (การที่เธรดหนึ่งอ่านข้อมูลขณะที่อีกเธรดกำลังเขียนไม่เสร็จ):

```csharp
public class TelemetryStateStore
{
    private readonly object _lock = new();
    private int _rawValue = 0;
    private DateTime _lastUpdated = DateTime.UtcNow;

    // ส่วนเขียน (Worker)
    public void Update(int val)
    {
        lock (_lock)
        {
            _rawValue = val;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    // ส่วนอ่าน (Web API)
    public (int Raw, DateTime Timestamp) GetLatest()
    {
        lock (_lock)
        {
            return (_rawValue, _lastUpdated);
        }
    }
}
```

### 3.4 ระบบ Dual-Mode (Auto-Detect Hardware & Fallback Simulation)
* ถ้าระบบค้นพบและเปิดพอร์ต COM สำเร็จ $\rightarrow$ เข้าสู่ **Live Hardware Mode**
* ถ้าไม่พบบอร์ด USB หรือพอร์ตถูกแย่ง $\rightarrow$ ไม่โยน Exception ให้ระบบล่ม แต่สลับเข้าสู่ **Simulation Mode** โดยคำนวณคลื่น Sine Wave อัตโนมัติ:
  $$\text{simulatedValue} = \left(\frac{\sin(\text{time} \times 2.0) + 1.0}{2.0}\right) \times 4095$$

---

## บทที่ 4: การแสดงผลเชิงภาพด้วย SVG และแผงควบคุมเรียลไทม์

### 4.1 ทำไมต้อง SVG ในงาน Industrial IoT?
1. **Resolution Independent (Vector Math):** คมชัด 100% ทุกหน้าจอ ไม่แตกเป็นพิกเซลเหมือน `<canvas>` หรือภาพบิตแมป
2. **DOM-Based & CSS Stoppable:** เป็น XML แท็ก สามารถดักจับ Event และใช้ CSS Transition ปรับความนุ่มนวลได้โดยตรง
3. **Zero External Dependency:** ไม่ต้องโหลดไลบรารีใหญ่ๆ (เช่น Chart.js, D3.js) รันได้แม้อุปกรณ์ Edge ไม่ได้เชื่อมต่ออินเทอร์เน็ต

### 4.2 สถาปัตยกรรม Client-Side Polling
* หน้าเว็บใช้คำสั่ง JavaScript `setInterval()` วนลูปส่งคำขอ `fetch('/api/telemetry')` ทุกๆ $200\text{ ms}$ (ความถี่ $5\text{ Hz}$)
* เมื่อได้ค่า JSON ก็นำไปอัปเดตสไตล์ของ SVG ทันที

```javascript
setInterval(async () => {
    try {
        const res = await fetch('/api/telemetry');
        const data = await res.json();
        // data = { rawValue: 2048, percentage: 50.0, voltage: 1.65 }
        updateDashboard(data);
    } catch (err) {
        console.error("Polling error:", err);
    }
}, 200);
```

### 4.3 องค์ประกอบของวิดเจ็ตแสดงผลมาตรฐาน
1. **Speedometer (เข็มไมล์):** ใช้แท็ก `<line>` หรือ `<polygon>` แล้วปรับมุมด้วย `transform: rotate(${angle}deg)` มีจุดศูนย์กลางหมุนด้วย `transform-origin`
2. **Circular Gauge / Radial Progress:** ใช้แท็ก `<circle>` ควบคุมความยาวเส้นขอบด้วย `stroke-dasharray` และ `stroke-dashoffset`
3. **VU Meter:** แถบสี่เหลี่ยม `<rect>` สไตล์เครื่องเสียง แบ่งย่าน เขียว (ปกติ) $\rightarrow$ เหลือง (เตือน) $\rightarrow$ แดง (อันตราย) ควบคุมด้วย `fill-opacity` (เปิด 1.0, ปิด 0.15)
4. **Retro 7-Segment Display:** ใช้แท็ก `<polygon>` 7 ชิ้น (a-g) กำหนดสถานะ On/Off ตามตารางเลขฐาน 0-9

---

## สรุปสาระสำคัญเชิงปฏิบัติการ (Lab 8.1 - 8.5)

### Lab 8.1: พื้นฐาน Kestrel และ Minimal API
* คำสั่ง CLI: `dotnet new web`, `dotnet run`, `dotnet watch`
* การทดสอบเบื้องต้น คืนค่า JSON ผ่าน Anonymous Object และการอ่านค่าจาก URL

### Lab 8.2: การอ่าน ADC ของ ESP32 และการสตรีมผ่าน UART
* **ฮาร์ดแวร์:** ต่อ Potentiometer (10k) ขากลางเข้ากับ **GPIO 34**
* **ข้อควรระวังสำคัญ:**
  1. แรงดัน ESP32 ทนได้สูงสุด **3.3V** ห้ามต่อขา VCC ของ Pot เข้า 5V (VIN) โดยเด็ดขาด
  2. ต้องใช้กลุ่ม **ADC1 (GPIO 32-39)** เท่านั้น เพราะ **ADC2** ติดขัดเรื่องการแชร์ฮาร์ดแวร์กับระบบ Wi-Fi
* **การกรองสัญญาณ (Moving Average):** อ่านค่าสะสม $N$ ตัวอย่างเพื่อตัด Noise สัญญาณรบกวน
* **การส่งข้อมูล:** สตรีมผ่าน Serial ที่ความเร็ว **115200 bps** สิ้นสุดแต่ละแพ็กเก็ตด้วยบรรทัดใหม่ `\n`

### Lab 8.3: Hardware Serial Bridge สู่ Web Gateway
* ติดตั้งแพ็กเกจ: `dotnet add package System.IO.Ports`
* สร้าง `TelemetryStateStore` ป้องกัน Concurrency
* สร้าง `SerialBridgeWorker : BackgroundService` ดักรับข้อมูลเบื้องหลัง
* Endpoint `/api/telemetry` ดึงค่าจาก State Store มาคำนวณ Voltage และ Percentage

### Lab 8.4: แดชบอร์ด Interactive SVG Speedometer
* วางไฟล์ `index.html` ไว้ในโฟลเดอร์ `wwwroot`
* เปิดใช้งาน Static Files: `app.UseFileServer();` (หรือ `app.UseDefaultFiles(); app.UseStaticFiles();`)
* แปลงค่า ADC สู่มุมองศาเข็มไมล์ ด้วยสูตร Linear Mapping
* ใส่ CSS Transition เพื่อให้เข็มหมุนอย่างนุ่มนวล:
  ```css
  #needle {
      transform-origin: 150px 150px;
      transition: transform 0.15s cubic-bezier(0.4, 0, 0.2, 1);
  }
  ```

### Lab 8.5: Dual-Channel Telemetry Command Center
* รองรับเซนเซอร์คู่ (Channel A: Hardware จริง, Channel B: Simulation Sine Wave หรือเซนเซอร์ตัวที่สอง)
* สูตรกำหนดประเภทวิดเจ็ตตามเลข 3 ตัวท้ายรหัสนักศึกษา ($N$):
  * เกจ์ซ้าย: $\text{Left} = (N \pmod 4) + 1$
  * เกจ์ขวา: $\text{Right} = (\lfloor N / 4 \rfloor \pmod 4) + 1$
  * กฎป้องกันเกจ์ซ้ำ: หากซ้ำกัน ให้บวกเกจ์ขวาเพิ่ม 1
* ชนิดวิดเจ็ต 4 แบบ: 1 = Analog Speedometer, 2 = Audio VU Meter, 3 = Retro 7-Segment, 4 = Liquid Level Tank

---

## สูตรคำนวณและคณิตศาสตร์ที่มักออกสอบ

### 1. สูตรการแปลงสเกลเชิงเส้น (Linear Mapping Formula)
การแปลงค่า $X$ จากช่วง $[In_{min}, In_{max}]$ ไปยังช่วง $[Out_{min}, Out_{max}]$:

$$Y = Out_{min} + \left( \frac{X - In_{min}}{In_{max} - In_{min}} \right) \times (Out_{max} - Out_{min})$$

* **ตัวอย่างการคำนวณมุมหมุนเข็มไมล์:**
  * กำหนดค่า ADC 12-bit: $In_{min} = 0, In_{max} = 4095$
  * กำหนดมุมหน้าปัด: $Out_{min} = -90^\circ, Out_{max} = +90^\circ$
  * ถ้าอ่านค่า $X = 2048$:
    $$Angle = -90 + \left( \frac{2048 - 0}{4095 - 0} \right) \times (90 - (-90)) = -90 + (0.5) \times 180 = 0^\circ \text{ (เข็มชี้ตรงกลาง)}$$

### 2. สูตรการแปลงแรงดันไฟฟ้า (ADC Voltage Calculation)
ความละเอียด 12-bit ($2^{12} = 4096$ ค่า คือช่วง $0 - 4095$):

$$\text{Voltage (V)} = \left(\frac{\text{ADC Raw}}{4095}\right) \times 3.3\text{ V}$$

* ตัวอย่าง: $\text{ADC} = 4095 \rightarrow 3.3\text{ V}$, $\text{ADC} = 0 \rightarrow 0.0\text{ V}$

### 3. สูตรความยาวส่วนโค้งวงกลม (Arc Length) และ Circular Progress
$$C_{\text{arc}} = 2 \times \pi \times R \times \left(\frac{\phi}{360^\circ}\right)$$

* กำหนดให้ $R$ คือรัศมีของวงกลม และ $\phi$ คือมุมกวาดของส่วนโค้ง (องศา)
* **การคำนวณค่า `stroke-dashoffset`:**
  $$\text{Ratio} = \frac{\text{Raw ADC}}{4095.0}$$
  $$\text{Offset} = C_{\text{arc}} \times (1 - \text{Ratio})$$
  * เมื่อ $\text{Ratio} = 0.0 \rightarrow \text{Offset} = C_{\text{arc}}$ (ซ่อนเส้นทั้งหมด)
  * เมื่อ $\text{Ratio} = 1.0 \rightarrow \text{Offset} = 0$ (แสดงเส้นเต็ม 100%)

---

## รวมประเด็นหลอกยอดฮิต & ข้อควรระวังในข้อสอบ (Trap & Pitfalls)

| ประเด็นข้อสอบ | ข้อที่ผิด / ความเข้าใจที่ผิด | สิ่งที่ถูกต้องตามหลักวิศวกรรม |
| :--- | :--- | :--- |
| **การเลือกขา ADC บน ESP32** | ใช้ขา GPIO ใดก็ได้ หรือใช้ขา ADC2 | **ต้องใช้กลุ่ม ADC1 (GPIO 32-39)** เท่านั้น เพราะ ADC2 จะใช้ไม่ได้เมื่อเปิดใช้สัญญาณ Wi-Fi |
| **ระดับแรงดันอินพุต ADC** | ป้อนไฟ 5V ได้เพราะชิปมีเรกูเลเตอร์ | **ห้ามเกิน 3.3V** เด็ดขาด ขา GPIO รับได้สูงสุด 3.3V การป้อน 5V ทำให้ชิปพังถาวร |
| **การเปิดพอร์ต Serial** | สั่ง `SerialPort.Open()` ในฟังก์ชัน Web API | **ผิดหลักการ!** ต้องใช้ `BackgroundService` เปิดพอร์ตค้างไว้เบื้องหลัง (Producer-Consumer) |
| **ความเร็ว Baud Rate** | ฝั่ง ESP32 ตั้ง 115200 แต่ C# ตั้ง 9600 | ข้อมูลจะกลายเป็นขยะ (Garbage Data) **ต้องตั้งให้ตรงกันทั้งสองฝั่ง (115200 bps)** |
| **หน่วยความจำร่วม (Shared State)** | ใช้ตัวแปร Global ธรรมดาโดยไม่มีตัวป้องกัน | เกิด **Race Condition** หรือ **Torn Read** ต้องครอบด้วย `lock` เสมอ |
| **การทำงานของ SVG ในเบราว์เซอร์** | SVG ขยายแล้วแตกเป็นเม็ดพิกเซลเหมือน Canvas | SVG เป็น **Vector Graphics (สมการคณิตศาสตร์)** คมชัดทุกความละเอียด 100% |
| **ความนุ่มนวลของเข็มไมล์** | ต้องส่งข้อมูลจากเซิร์ฟเวอร์ด้วยความถี่ 100 Hz | ไม่จำเป็น ส่งข้อมูล $5\text{ Hz}$ ($200\text{ ms}$) แล้วใช้ **CSS Transition** เกลี่ยความนุ่มนวล |

---

## แนวข้อสอบทฤษฎีและปฏิบัติ (Exam Cheat Sheet & Q&A)

### ข้อที่ 1 (ทฤษฎีสถาปัตยกรรม): จงอธิบายข้อได้เปรียบของการใช้ Kestrel Web Server เทียบกับ LAMP Stack ในงาน Edge IoT
**แนวคำตอบ:**
1. Kestrel ทำงานแบบ **In-Process Single Binary** มีเว็บเซิร์ฟเวอร์ฝังในตัวเอง ไม่ต้องติดตั้งหรือคอนฟิก Apache/PHP เพิ่มเติม
2. ใช้รูปแบบ **Asynchronous Non-blocking I/O** (ร่วมกับ System.IO.Pipelines) ทำให้กินแรมต่อการเชื่อมต่อน้อยมาก (~2-4 KB ต่อ Socket) เทียบกับ LAMP ที่ต้องจอง Process ละ 25 MB
3. ช่วยแก้ปัญหา **The C10K Problem** โดยไม่เกิด Thread Exhaustion และลด Context Switching Overhead ของ CPU ส่งผลให้อุปกรณ์ Edge ที่มีทรัพยากรจำกัดสามารถให้บริการโหลดข้อมูลเซนเซอร์ได้อย่างเสถียร
4. สามารถเชื่อมต่อฮาร์ดแวร์ระดับต่ำ (เช่น พอร์ต Serial/UART) ได้โดยตรงในโปรเซสเดียวกันอย่างไร้รอยต่อ

### ข้อที่ 2 (การจัดการ Concurrency): ทำไมการเปิด-ปิด Serial Port ภายในฟังก์ชัน Request Handler ของ Web API จึงเป็นแนวทางที่ผิด และควรแก้ปัญหาอย่างไร?
**แนวคำตอบ:**
* **สาเหตุที่ผิด:**
  1. การเปิด Virtual COM Port มีความล่าช้า (Latency 10-50 ms) ทำให้การตอบสนองเว็บช้าลงอย่างมาก
  2. พอร์ตสื่อสารอนุกรมถูกจำกัดให้ใช้งานได้เพียงครั้งละ 1 เธรด หากมี Client เรียกเข้ามาพร้อมกันจะเกิดข้อผิดพลาด `UnauthorizedAccessException`
  3. เกิดข้อมูลสูญหาย (Buffer Overflow) เนื่องจากช่วงที่ปิดพอร์ต ข้อมูลที่ไมโครคอนโทรลเลอร์ส่งมาจะถูกทิ้งไป
* **แนวทางแก้ไข:**
  ประยุกต์ใช้ **Producer-Consumer Pattern** โดยสร้าง `BackgroundService` (Worker Thread) ขึ้นมาเปิดพอร์ต Serial เพียงครั้งเดียวและวนลูปอ่านข้อมูลอยู่เบื้องหลัง แล้วนำค่ามาบันทึกลงในคลังข้อมูลส่วนกลาง (`TelemetryStateStore`) ที่ปกป้องด้วย `lock` (Thread-Safety) จากนั้นเมื่อมี Web Request เข้ามา Client จะอ่านค่าจากหน่วยความจำกลางนี้ไปใช้งานได้ทันที

### ข้อที่ 3 (คำนวณเชิงเส้น): หากอ่านค่า ADC จาก ESP32 ได้ 3072 จงคำนวณหา (ก) แรงดันไฟฟ้า (Voltage) และ (ข) มุมการหมุนของเข็มไมล์ในช่วง -90° ถึง +90°
**แนวคำตอบ:**
* (ก) แรงดันไฟฟ้า:
  $$\text{Voltage} = \left(\frac{3072}{4095}\right) \times 3.3\text{ V} \approx 0.7502 \times 3.3\text{ V} \approx \mathbf{2.48\text{ V}}$$
* (ข) มุมการหมุนของเข็มไมล์:
  $$\text{Angle} = -90 + \left(\frac{3072 - 0}{4095 - 0}\right) \times (90 - (-90)) = -90 + (0.7502 \times 180) = -90 + 135.03 \approx \mathbf{+45.03^\circ}$$

### ข้อที่ 4 (ฮาร์ดแวร์): เหตุใดในการทดลองอ่านค่าสัญญาณแอนะล็อกของ ESP32 จึงต้องเลือกต่อวงจรที่ขา GPIO 34 แทนที่จะเป็น GPIO 2 หรือ GPIO 4?
**แนวคำตอบ:**
เพราะบนชิป ESP32 วงจรแปลงสัญญาณแอนะล็อกแบ่งออกเป็น 2 โมดูล คือ **ADC1** (GPIO 32-39) และ **ADC2** (GPIO 0, 2, 4, 12-15, 25-27) โดยโมดูล ADC2 มีข้อจำกัดร้ายแรงคือใช้ทรัพยากรฮาร์ดแวร์ร่วมกับโมดูลวิทยุ Wi-Fi ดังนั้นหากระบบมีการเปิดใช้งาน Wi-Fi ขาในกลุ่ม ADC2 จะไม่สามารถอ่านค่าแอนะล็อกได้ การเลือกใช้ **GPIO 34 ซึ่งอยู่ในกลุ่ม ADC1** จึงเป็นมาตรฐานที่ถูกต้องและป้องกันข้อผิดพลาดในการต่อยอดระบบ IoT เสมอ

### ข้อที่ 5 (วิเคราะห์โค้ด C#): จงเขียนโค้ด Minimal API สั้นๆ ที่รับค่าผ่าน Query Parameter ชื่อ `speed` และ Route Parameter ชื่อ `id` แล้วคืนค่ากลับเป็น JSON
**แนวคำตอบ:**
```csharp
app.MapGet("/api/motor/{id}", (int id, int speed) => {
    int safeSpeed = Math.Clamp(speed, 0, 100);
    return Results.Ok(new {
        motorId = id,
        targetSpeed = safeSpeed,
        timestamp = DateTime.UtcNow
    });
});
```

---

## อภิธานศัพท์สำคัญ (Glossary)

| คำศัพท์ทางเทคนิค | ความหมายในบริบทระบบ Edge Web Server & IoT |
| :--- | :--- |
| **Kestrel** | Web Server สมรรถนะสูงแบบ Cross-platform และ Non-blocking I/O ที่ฝังตัวใน .NET Core |
| **Self-Hosted** | สถาปัตยกรรมที่แอปพลิเคชันมีเอนจินของเว็บเซิร์ฟเวอร์อยู่ในตัวเอง รันเป็นไบนารีเดียวได้โดยตรง |
| **Minimal API** | รูปแบบการเขียน REST API ใน .NET ยุคใหม่ที่ลดทอน Boilerplate code และเพิ่มความเร็วในการตอบสนอง |
| **Zero-Allocation** | เทคนิคการเขียนโปรแกรมที่ไม่สร้างออบเจกต์ขยะบน Heap เพื่อลดภาระการทำงานของ Garbage Collector |
| **System.IO.Pipelines** | ไลบรารีประสิทธิภาพสูงสำหรับจัดการบัฟเฟอร์ข้อมูลเครือข่ายความเร็วสูงแบบ Zero-copy |
| **System.IO.Ports** | แพ็กเกจของ .NET สำหรับจัดการสื่อสารกับ Serial COM Port / USB UART ของระบบปฏิบัติการ |
| **BackgroundService** | คลาสพื้นฐานสำหรับรัน Background Worker Thread ระยะยาว แยกอิสระจากเธรดของ Web Server |
| **Producer-Consumer** | แบบแผนการออกแบบที่แยกส่วนผู้ผลิตข้อมูล (Serial Worker) ออกจากส่วนผู้นำข้อมูลไปใช้ (Web API) |
| **Thread-Safety** | คุณสมบัติของโค้ดหรือหน่วยความจำที่อนุญาตให้หลายเธรดเข้าถึงพร้อมกันได้อย่างถูกต้องและปลอดภัย |
| **Race Condition** | ภาวะแย่งชิงทรัพยากรที่เกิดจากการที่หลายเธรดพยายามอ่านและเขียนตัวแปรเดียวกันพร้อมกันจนค่าเสียหาย |
| **Torn Read** | สภาวะที่เธรดหนึ่งอ่านข้อมูลออกมาได้เพียงครึ่งเดียว เนื่องจากอีกเธรดกำลังเขียนทับข้อมูลลงไปในจังหวะเดียวกัน |
| **Polling** | กลไกที่ไคลเอนต์ส่งคำขอถามข้อมูลจากเซิร์ฟเวอร์ซ้ำๆ เป็นระยะตามช่วงเวลาที่กำหนด (เช่น ทุก 200 ms) |
| **Linear Mapping** | การแปลงสัดส่วนเชิงเส้นจากช่วงค่าเดิม (เช่น ADC 0-4095) ไปยังช่วงค่าเป้าหมาย (เช่น มุม -90° ถึง +90°) |
| **SVG** | กราฟิกเวกเตอร์สองมิติที่อิงโครงสร้าง XML ขยายขนาดได้ไม่จำกัดและควบคุมผ่าน DOM และ CSS ได้โดยตรง |
| **ADC (Analog-to-Digital)** | วงจรแปลงแรงดันไฟฟ้าแอนะล็อกต่อเนื่อง (0-3.3V) ให้กลายเป็นค่าตัวเลขดิจิทัล (0-4095 สำหรับ 12-bit) |
| **Moving Average** | การคำนวณค่าเฉลี่ยเคลื่อนที่เพื่อกรองและลดสัญญาณรบกวน (Noise Filter) ของเซนเซอร์แอนะล็อก |
| **CORS** | นโยบายความปลอดภัยของเบราว์เซอร์ที่ควบคุมการเข้าถึงทรัพยากรข้ามโดเมน (Cross-Origin Resource Sharing) |
| **Fallback Simulation** | กลไกสำรองที่สร้างสัญญาณจำลองขึ้นอัตโนมัติเมื่อระบบตรวจไม่พบฮาร์ดแวร์จริง เพื่อไม่ให้ระบบล่ม |
| **The C10K Problem** | ความท้าทายของระบบเว็บเซิร์ฟเวอร์ในการรองรับการเชื่อมต่อพร้อมกัน 10,000 การเชื่อมต่อบนทรัพยากรที่จำกัด |

---
> 💡 **คำแนะนำสำหรับการนำไปวางใน Microsoft Word:**  
> สามารถคัดลอกเนื้อหาทั้งหมดในเอกสารนี้ แล้วกด Paste (หรือ Paste and Match Style) ใน Microsoft Word ได้ทันที โปรแกรม Word จะแปลงหัวข้อ, ตารางเปรียบเทียบ, รายการบุลเล็ต และบล็อกโค้ดตัวอย่างให้โดยอัตโนมัติอย่างสวยงาม
