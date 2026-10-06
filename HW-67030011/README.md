# การส่งงานสัปดาห์ที่ 8: Kestrel Edge Web Server & ESP32 Serial Bridge

* **รหัสนักศึกษา:** 67030011
* **Git Branch:** `HW-67030011`
* **สภาพแวดล้อม:** macOS, .NET SDK 9.0.301, ESP-IDF v6.0.2 (target `esp32`)

---

## สารบัญงานและรายงานผลการทดลอง

### 1. ใบงาน 8.1 — Kestrel Minimal API Fundamentals
- [06-Labsheet-08-1-Kestrel-Minimal-API-Fundamentals.md](../06-Labsheet-08-1-Kestrel-Minimal-API-Fundamentals.md)
- Endpoint `/`, `/api/status`, `/api/led/{state}` และ Micro-Challenge `/api/student`
- ตอบ Checkpoint 1.1 และคำถามท้ายการทดลองครบ 3 ข้อ
- ข้อสังเกต: พอร์ต 5000 บน macOS ถูก AirPlay Receiver จองไว้ (ใช้ 5080 แทน) และ `DateTime.ToString()` บนเครื่อง Locale ไทยได้ปี พ.ศ. 2569

### 2. ใบงาน 8.2 — ESP32 ADC & Serial Stream
- [07-Labsheet-08-2-ESP32-ADC-and-Serial-Stream.md](../07-Labsheet-08-2-ESP32-ADC-and-Serial-Stream.md)
- เฟิร์มแวร์ `adc_oneshot` GPIO 34 + EMA Filter (α = 0.25, 20 Hz) Build ผ่านด้วย ESP-IDF v6.0.2
- ตอบ Checkpoint 2.1 ครบ 2 ข้อ

### 3. ใบงาน 8.3 — Hardware Serial Bridge to Web
- [08-Labsheet-08-3-Hardware-Serial-Bridge-to-Web.md](../08-Labsheet-08-3-Hardware-Serial-Bridge-to-Web.md)
- `TelemetryStateStore` + `SerialBridgeWorker` + `/api/telemetry` พร้อม Micro-Challenge `alertLevel`
- แก้ปัญหาที่พบในโค้ดใบงาน: `using` กลางไฟล์ (CS1529), การเลือกพอร์ตบน macOS และค่าค้างเป็น "Live" ตลอดเมื่อสายหลุดโดย OS ไม่โยน Exception (เพิ่ม Data Watchdog 3 วินาที)

### 4. ใบงาน 8.4 — Interactive SVG Speedometer & Creative Playground
- [09-Labsheet-08-4-Interactive-SVG-Speedo-and-Creative-Dashboards.md](../09-Labsheet-08-4-Interactive-SVG-Speedo-and-Creative-Dashboards.md)
- Speedometer SVG + Creative Playground ตัวเลือก A (VU Meter) + Badge `alertLevel`

### 5. ใบงาน 8.5 — Dual-Channel IoT Command Center
- [10-Labsheet-08-5-Dual-Sensor-Command-Center.md](../10-Labsheet-08-5-Dual-Sensor-Command-Center.md)
- N = 011 → เกจ์ซ้าย **4 Liquid Level Tank** / เกจ์ขวา **3 Retro 7-Segment**
- รองรับข้อมูลทั้งแบบ `A` (B จำลอง) และ `A,B` (Bonus: เซนเซอร์จริงตัวที่ 2 ที่ GPIO 35)

---

## รายการซอร์สโค้ดโปรเจกต์ (Source Code)

| ใบงาน | ESP32 (ESP-IDF C) | Kestrel (.NET 9) |
| :--- | :--- | :--- |
| 8.1 | — | [Lab8-1/Program.cs](Lab8-1/Program.cs) |
| 8.2 | [Lab8-2/ESP32_ADC_Stream/main/main.c](Lab8-2/ESP32_ADC_Stream/main/main.c) | — |
| 8.3 | [Lab8-3/ESP32_ADC_Stream/main/main.c](Lab8-3/ESP32_ADC_Stream/main/main.c) | [Lab8-3/Kestrel_Serial_Gateway/Program.cs](Lab8-3/Kestrel_Serial_Gateway/Program.cs) |
| 8.4 | [Lab8-4/ESP32_ADC_Stream/main/main.c](Lab8-4/ESP32_ADC_Stream/main/main.c) | [Lab8-4/Kestrel_SVG_Dashboard/](Lab8-4/Kestrel_SVG_Dashboard/) (`Program.cs`, `wwwroot/index.html`) |
| 8.5 | [Lab8-5/ESP32_ADC_Stream/main/main.c](Lab8-5/ESP32_ADC_Stream/main/main.c) | [Lab8-5/Kestrel_Dual_Dashboard/](Lab8-5/Kestrel_Dual_Dashboard/) (`Program.cs`, `wwwroot/index.html`) |

### วิธีรัน

```bash
# ESP32 (ตัวอย่าง Lab 8-5)
cd HW-67030011/Lab8-5/ESP32_ADC_Stream
idf.py set-target esp32
idf.py -p /dev/cu.usbserial-XXXX flash monitor   # Windows: -p COMxx แล้วกด Ctrl+] เพื่อออกก่อนรัน Kestrel

# Kestrel (ตัวอย่าง Lab 8-5) -> เปิด http://localhost:5080
cd HW-67030011/Lab8-5/Kestrel_Dual_Dashboard
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 dotnet run                 # เลือกพอร์ต USB อัตโนมัติ
LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8 dotnet run -- --Serial:Port=/dev/cu.usbserial-XXXX   # หรือระบุพอร์ตเอง
```

> `LANG=en_US.UTF-8` จำเป็นเฉพาะเครื่องที่ตั้ง Region เป็นไทย (ดูรายละเอียดในใบงาน 8.4) บน Windows สั่ง `dotnet run` ได้ตามปกติ

---

## รูปภาพหลักฐานผลการทดลอง (Images)

- [Images/](Images/)
  - `lab8-1-activity2-welcome.png` — หน้า `/` แสดงชื่อนักศึกษา
  - `lab8-1-activity3-status-json.png` — JSON จาก `/api/status`
  - `lab8-1-activity4-led-on.png`, `lab8-1-activity4-led-off.png` — Route Parameter `/api/led/{state}`
  - `lab8-1-challenge-student.png` — Micro-Challenge `/api/student`
  - `lab8-3-telemetry-simulation.png` — `/api/telemetry` ในโหมดจำลอง พร้อม `alertLevel`
  - `lab8-4-dashboard-1.png`, `lab8-4-dashboard-2.png`, `lab8-4-dashboard-3.png` — Speedometer + VU Meter
  - `lab8-5-dashboard-desktop.png`, `lab8-5-dashboard-desktop-2.png` — Dual Dashboard โหมดจำลอง 2 ช่อง
  - `lab8-5-dashboard-live-emulated.png` — Dual Dashboard โหมด Live + Sim B (บอร์ดจำลองผ่าน pseudo-terminal)
