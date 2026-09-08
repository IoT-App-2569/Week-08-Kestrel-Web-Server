# แผนการพัฒนาโค้ด Week 08 — Kestrel Edge Web Server
**Branch:** `67030098-HW` | **รหัสนักศึกษา:** 67030098

---

## 1. โครงสร้างโฟลเดอร์เป้าหมาย (Target Layout)

```text
Code/
├── PLAN.md                              # ไฟล์นี้
├── Lab8-1/
│   └── Kestrel_API/                     # dotnet new web
│       ├── Kestrel_API.csproj
│       └── Program.cs
├── Lab8-2/
│   └── ESP32_ADC_Stream/                # idf.py create-project
│       ├── CMakeLists.txt
│       └── main/
│           ├── CMakeLists.txt
│           └── main.c
├── Lab8-3/
│   ├── ESP32_ADC_Stream/                # คัดลอกจาก Lab8-2 (ต้องมีสำเนาแยก)
│   └── Kestrel_Serial_Gateway/
│       ├── Kestrel_Serial_Gateway.csproj   # + System.IO.Ports
│       └── Program.cs                      # StateStore + Worker + /api/telemetry
└── Lab8-4/
    ├── ESP32_ADC_Stream/                # คัดลอกจาก Lab8-3
    └── Kestrel_SVG_Dashboard/
        ├── Kestrel_SVG_Dashboard.csproj
        ├── Program.cs                   # = Lab8-3 + app.UseFileServer()
        └── wwwroot/
            └── index.html               # SVG Speedometer + fetch polling
```

> **กฎสำคัญจากใบงาน:** ห้ามใช้โฟลเดอร์ร่วมกันระหว่างแล็บ (Project Isolation / Anti-Cheating)
> แต่ละแล็บต้องมีสำเนาโปรเจกต์ของตัวเอง และ **commit แยกทีละแล็บ**

---

## 2. แผนงานรายใบงาน (Per-Lab Build Plan)

### Lab 8-1 — Kestrel Minimal API Fundamentals
**เป้าหมาย:** เว็บเซิร์ฟเวอร์ตัวแรก + JSON อัตโนมัติ + route parameter

| ขั้น | งานที่ทำ | ไฟล์ |
| :-- | :-- | :-- |
| 1 | `dotnet new web -o Code/Lab8-1/Kestrel_API` | `.csproj` |
| 2 | `GET /` คืนข้อความต้อนรับพร้อมชื่อนักศึกษา | `Program.cs` |
| 3 | `GET /api/status` คืน anonymous object แล้วให้ Kestrel serialize เป็น JSON อัตโนมัติ (`gateway`, `status`, `uptimeSeconds`, `isHealthy`) | `Program.cs` |
| 4 | `GET /api/led/{state}` รับ route parameter + `Console.WriteLine` log ลง terminal | `Program.cs` |
| 5 | **Micro-Challenge:** `GET /api/student` คืน `studentId`, `studentName`, `faculty`, `targetSensor`, `timestamp` | `Program.cs` |

**เกณฑ์ผ่าน:** เปิด `/api/status` และ `/api/student` เห็น JSON ครบถ้วน, terminal พิมพ์ `[HH:mm:ss] LED Control: on/off`

---

### Lab 8-2 — ESP32 ADC & Serial Stream
**เป้าหมาย:** เฟิร์มแวร์ ESP-IDF v6.x อ่าน ADC 12-bit แล้วสตรีมออก UART

| ขั้น | งานที่ทำ | หมายเหตุ |
| :-- | :-- | :-- |
| 1 | สร้างโปรเจกต์ `ESP32_ADC_Stream` (native หรือ docker `espressif/idf:release-v6.1`) | `idf.py create-project` |
| 2 | เขียน `main/main.c` ใช้ไดรเวอร์ `esp_adc/adc_oneshot.h` | ADC_UNIT_1 |
| 3 | ตั้งค่า `ADC_BITWIDTH_12` + `ADC_ATTEN_DB_12` (ช่วง 0–3.3V → 0–4095) | |
| 4 | เลือกช่องสัญญาณตามชิป: ESP32 classic → `ADC_CHANNEL_4` (**GPIO 32**), ESP32-C6 → `ADC_CHANNEL_4` (GPIO 4) | ใช้ `#if CONFIG_IDF_TARGET_ESP32C6` |
| 5 | ลูปหลัก: `printf("%d\n", val)` ทีละบรรทัด — รูปแบบง่ายที่สุดให้ฝั่ง C# `ReadLine()` แยกได้ | |
| 6 | เพิ่ม **EMA filter** `alpha = 0.25f` และเร่งเป็น 20 Hz (`vTaskDelay(50ms)`) | ลด noise |

**วงจรที่ต่อจริง (Potentiometer B50K):**
`3.3V → pot ขา 1` | `GND → pot ขา 3` | **`GPIO 32` (ADC1_CHANNEL_4) → pot ขา 2 (wiper)**

**ห้ามเด็ดขาด:** ต่อ pot เข้า 5V/VIN (ขา GPIO ทนได้แค่ 3.3V)

**เกณฑ์ผ่าน:** monitor เห็นเลขไหลจาก ~0 ถึง ~4095 เมื่อหมุนสุดสองทาง แล้วกด `Ctrl+]` ปลดล็อกพอร์ต COM

---

### Lab 8-3 — Hardware Serial Bridge to Web
**เป้าหมาย:** ต่อ ESP32 เข้า Kestrel ผ่าน BackgroundService แล้วเสิร์ฟเป็น REST API

| ขั้น | งานที่ทำ | คลาส/สัญลักษณ์ |
| :-- | :-- | :-- |
| 1 | `dotnet new web` + `dotnet add package System.IO.Ports` | |
| 2 | คลาสเก็บสถานะแบบ thread-safe (`lock`) พร้อมคำนวณ voltage/percent ตอนอ่าน | `TelemetryStateStore` |
| 3 | `BackgroundService` สแกนพอร์ต → เปิดที่ 115200 bps → `ReadLine()` → `Update()` | `SerialBridgeWorker` |
| 4 | **Dual-Mode Fallback:** ถ้าเปิดพอร์ตไม่ได้/ไม่มีพอร์ต ให้ปั่น sine wave 20 รอบ (2 วินาที) แล้ววนกลับไปสแกนใหม่ | |
| 5 | ลงทะเบียน DI: `AddSingleton<TelemetryStateStore>()` + `AddHostedService<SerialBridgeWorker>()` | |
| 6 | `GET /api/telemetry` คืน `sensor`, `rawValue`, `voltage`, `percentage`, `dataSource`, `timestamp` | |
| 7 | **Micro-Challenge:** เพิ่มฟิลด์ `alertLevel` → มากกว่า 85 = `DANGER (HIGH)`, 70–85 = `WARNING`, ต่ำกว่า 70 = `NORMAL` | |

**จุดที่ต้องปรับให้ตรงกับเครื่อง:** ตัวแปร `targetPort` (ใบงานฮาร์ดโค้ดไว้เป็น `"COM24"`) — จะทำให้อ่านจาก config/environment variable ได้ และ fallback เป็น auto-detect เพื่อไม่ต้องแก้โค้ดทุกครั้งที่เปลี่ยนเครื่อง

**เกณฑ์ผ่าน:** หมุน pot → refresh เบราว์เซอร์ → ตัวเลขเปลี่ยนตาม; ถอดสาย USB → `dataSource` เปลี่ยนเป็น Simulation Mode โดยเซิร์ฟเวอร์ไม่ล่ม

---

### Lab 8-4 — Interactive SVG Speedometer Dashboard
**เป้าหมาย:** หน้าเว็บ SVG แบบ zero-dependency ที่เข็มกวาดตามมือหมุน

| ขั้น | งานที่ทำ | ไฟล์ |
| :-- | :-- | :-- |
| 1 | คัดลอกโครงจาก Lab8-3 → `Kestrel_SVG_Dashboard` + สร้าง `wwwroot` | |
| 2 | เพิ่ม `app.UseFileServer();` ก่อนบรรทัด `app.Run();` | `Program.cs` |
| 3 | สร้างหน้าปัด Speedometer ด้วย `<svg>` ล้วน (arc, tick marks, เข็ม) | `wwwroot/index.html` |
| 4 | Linear mapping: `percent → angle` ช่วง `-90°` ถึง `+90°` | `percentToAngle()` |
| 5 | CSS `transition` บน `transform: rotate()` ให้เข็มลื่นไหล ไม่กระตุก | |
| 6 | JS polling ด้วย `fetch("/api/telemetry")` ทุก **150 ms** อัปเดต `disp-percent`, `disp-raw`, `disp-volt`, `disp-source`, `needle` | `setInterval` |
| 7 | **Creative Playground (เลือกอย่างน้อย 1):** VU Meter 10 ดวง / 7-Segment 2 หลัก / Liquid Tank | |

**เกณฑ์ผ่าน:** เปิด `http://localhost:5000` เห็นหน้าปัด, หมุน pot แล้วเข็มขยับ near real-time โดยไม่ต้อง refresh

---

## 3. ลำดับการทำงาน (Execution Order)

```text
8-1 (พื้นฐาน API)
  └─> 8-2 (ฝั่งฮาร์ดแวร์ ผลิตข้อมูล)
        └─> 8-3 = 8-1 + 8-2 (บริดจ์ Serial → API)   ◄── หัวใจของสัปดาห์
              └─> 8-4 = 8-3 + wwwroot (SVG UI)
```

โค้ดฝั่ง Kestrel เป็นแบบ **สะสมต่อยอด**: `Program.cs` ของ 8-4 คือของ 8-3 บวก `UseFileServer()`
ดังนั้นควรทำ 8-3 ให้แน่นก่อน แล้วค่อยคัดลอกไปต่อยอดที่ 8-4

**แผน commit (ตามกฎ Anti-Cheating ที่ต้องแยก commit รายแล็บ):**
1. `feat(lab8-1): Kestrel minimal API with student endpoint`
2. `feat(lab8-2): ESP32 ADC oneshot serial stream with EMA filter`
3. `feat(lab8-3): serial bridge worker + telemetry API with alertLevel`
4. `feat(lab8-4): SVG speedometer dashboard with fetch polling`

---

## 4. สัญญาข้อมูล (Data Contract) — ใช้ร่วมกันระหว่าง 8-3 / 8-4

```jsonc
// GET /api/telemetry
{
  "sensor":     "ESP32-Potentiometer",
  "rawValue":   2048,                        // 0 - 4095 (ADC 12-bit)
  "voltage":    1.65,                        // raw / 4095 * 3.3
  "percentage": 50.0,                        // raw / 4095 * 100
  "alertLevel": "NORMAL",                    // Micro-Challenge 8-3
  "dataSource": "Live Hardware (COM24)",     // หรือ "Simulation Mode (Sine Wave)"
  "timestamp":  "2026-09-08T09:30:00.000Z"
}
```

**Serial line protocol (ESP32 → PC):** เลขจำนวนเต็มหนึ่งค่าต่อบรรทัด ปิดท้ายด้วย `\n` ที่ 115200 bps

---

## 5. สิ่งที่ต้องเตรียม/ตรวจสอบก่อนเริ่ม (Blockers)

| รายการ | สถานะที่ตรวจพบบนเครื่องนี้ | สิ่งที่ต้องทำ |
| :-- | :-- | :-- |
| .NET SDK | ✅ **มีแล้ว — v10.0.400** ที่ `C:\Program Files\dotnet\dotnet.exe` | ใช้ได้เลย (ใบงานรับ `10.0.xxx`) |
| Docker (สำหรับ ESP-IDF) | ยังตรวจไม่พบคำสั่ง `docker` | ติดตั้ง Docker Desktop **หรือ** ใช้ ESP-IDF native toolchain |
| พอร์ต COM ของ ESP32 | ยังไม่พบพอร์ตใดๆ | เสียบบอร์ด ESP32 แล้วตรวจใน Device Manager |
| Potentiometer 10 kΩ + breadboard | ต้องเตรียมเอง | ต่อวงจรตาม Lab 8-2 |

### ข้อจำกัดของ environment ที่ Claude รันอยู่
`dotnet build` ทำงานได้ปกติ แต่ **`dotnet run` รันไม่ได้** — Windows Application Control policy
บล็อกการโหลด `.dll` ที่เพิ่งคอมไพล์ใหม่ (`FileLoadException 0x800711C7`)
ดังนั้น **การทดสอบรันจริงต้องทำจาก terminal ของนักศึกษาเอง** ส่วน Claude ตรวจได้แค่ระดับคอมไพล์

> **ยังเขียนโค้ดต่อได้ทันทีแม้ยังไม่มีฮาร์ดแวร์** — Lab 8-3/8-4 มี Simulation Mode ในตัว
> ส่วนที่ต้องรอของจริงคือการ build/flash ESP32 (Lab 8-2) และการทดสอบ Live Mode เท่านั้น

---

## 6. หมายเหตุเพิ่มเติม

- **โปรเจกต์ `minimal_api/` ที่สร้างไว้แล้ว** อยู่ที่ **root ของรีโป** ไม่ใช่ใน `Code/Lab8-1/`
  และใช้ชื่อ `minimal_api` แทน `Kestrel_API` — ตัวโค้ดใช้งานได้ปกติ แต่**ผิดจากโครงสร้าง
  ส่งงานในหัวข้อที่ 8 ของ `Readme.md`** ควรย้ายไปเป็น `Code/Lab8-1/Kestrel_API/` ก่อนส่ง
  (`TargetFramework` เป็น `net10.0` ซึ่งใช้ได้ เพราะใบงานระบุว่ารับ `10.0.xxx`)

- **ใบงานที่ 8-5 มีอยู่ในรีโป** ([10-Labsheet-08-5](../10-Labsheet-08-5-Dual-Sensor-Command-Center.md)) แต่อยู่นอกขอบเขต "4 แล็บ" ที่วางแผนไว้นี้
  หากจะทำเพิ่ม: รหัส **67030098** → N = 98
  - เกจ์ซ้าย = (98 mod 4) + 1 = 2 + 1 = **3** → **Retro 7-Segment**
  - เกจ์ขวา = (⌊98/4⌋ mod 4) + 1 = (24 mod 4) + 1 = 0 + 1 = **1** → **Analog Speedometer**
  - ทั้งสองไม่ซ้ำกัน จึงไม่ต้องใช้กฎ Anti-Collision
- `.gitignore` ครอบคลุม `bin/`, `obj/`, `build/`, `sdkconfig.old` อยู่แล้ว — ไม่ต้องแก้เพิ่ม
- `Readme.md` อ้างถึงโฟลเดอร์ `Example_codes/Lab8-1` ถึง `Lab8-4` แต่ **ไม่มีอยู่จริง** ในรีโป จึงต้องเขียนโค้ดทั้งหมดจากใบงานโดยตรง
