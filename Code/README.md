# Code — ใบงานปฏิบัติการสัปดาห์ที่ 8
**รหัสนักศึกษา:** 67030098 | **Branch:** `67030098-HW`

โครงสร้างโฟลเดอร์เป็นไปตามข้อกำหนด **Project Isolation / Anti-Cheating**
ในหัวข้อที่ 8 ของ [Readme.md](../Readme.md) — แต่ละใบงานแยกโฟลเดอร์เด็ดขาด และ commit แยกทีละแล็บ

---

## โครงสร้างทั้งหมด

```text
Code/
├── PLAN.md                                  แผนการพัฒนา
├── README.md                                ไฟล์นี้
│
├── Lab8-1/
│   └── Kestrel_API/                         ✅ เสร็จสมบูรณ์
│       ├── Kestrel_API.csproj               net10.0, Microsoft.NET.Sdk.Web
│       ├── Program.cs                       4 endpoints + Micro-Challenge
│       ├── appsettings.json
│       └── Properties/launchSettings.json   พอร์ต 5277 / 7276
│
├── Lab8-2/
│   └── ESP32_ADC_Stream/                    ✅ เสร็จสมบูรณ์ (ESP-IDF v6.x)
│       ├── CMakeLists.txt                   project.cmake ระดับบนสุด
│       └── main/
│           ├── CMakeLists.txt               REQUIRES esp_adc
│           └── main.c                       ADC oneshot + EMA filter 20 Hz
│
├── Lab8-3/
│   ├── ESP32_ADC_Stream/                    สำเนาจาก Lab8-2 (เฟิร์มแวร์เดิม)
│   └── Kestrel_Serial_Gateway/              ✅ เสร็จสมบูรณ์
│       ├── Kestrel_Serial_Gateway.csproj    + System.IO.Ports 10.0.11
│       ├── Program.cs                       StateStore + Worker + /api/telemetry
│       └── appsettings.json                 ตั้งค่าพอร์ต COM ที่นี่
│
├── Lab8-4/
│   ├── ESP32_ADC_Stream/                    สำเนาจาก Lab8-2 (เฟิร์มแวร์เดิม)
│   └── Kestrel_SVG_Dashboard/               ✅ เสร็จสมบูรณ์
│       ├── Kestrel_SVG_Dashboard.csproj     + System.IO.Ports 10.0.11
│       ├── Program.cs                       = Lab8-3 + app.UseFileServer()
│       ├── appsettings.json
│       └── wwwroot/
│           └── index.html                   Speedometer + 7-Segment (2 หน้าปัด)
│
└── Lab8-5/
    ├── REPORT.md                            รายงานผลการทดลองตาม Rubric
    ├── ESP32_ADC_Stream/                    สำเนาจาก Lab8-2 (เฟิร์มแวร์เดิม)
    └── Kestrel_Dual_Dashboard/              ✅ เสร็จสมบูรณ์
        ├── Kestrel_Dual_Dashboard.csproj    + System.IO.Ports 10.0.11
        ├── Program.cs                       DualChannelStateStore + DualSerialBridgeWorker
        ├── appsettings.json
        └── wwwroot/
            └── index.html                   7-Segment (CH-A) + Speedometer (CH-B)
```

### หน้าปัดของแต่ละใบงาน

| ใบงาน | หน้าปัดซ้าย | หน้าปัดขวา | แหล่งข้อมูล |
| :-- | :-- | :-- | :-- |
| **8-4** | Analog Speedometer | Retro 7-Segment | ช่องเดียว — ทั้งคู่แสดงค่าเดียวกัน |
| **8-5** | Retro 7-Segment (CH-A) | Analog Speedometer (CH-B) | **2 ช่องแยกกัน** |

ใบงาน 8-5 กำหนดคู่เกจ์จากรหัสนักศึกษา **67030098** ($N = 98$):
เกจ์ซ้าย $= (98 \bmod 4) + 1 = 3$ (7-Segment), เกจ์ขวา $= (\lfloor 98/4 \rfloor \bmod 4) + 1 = 1$ (Speedometer)

> คำตอบของทุก Checkpoint / Review Question / Micro-Challenge อยู่ที่
> [../Answer/ANSWERS.md](../Answer/ANSWERS.md)

---

## วิธีรันแต่ละใบงาน

### Lab 8-1 — Kestrel Minimal API

```bash
dotnet run --project Code/Lab8-1/Kestrel_API
```

| Endpoint | ผลลัพธ์ |
| :-- | :-- |
| `/` | ข้อความต้อนรับ (text/plain) |
| `/api/status` | JSON สถานะเกตเวย์ + uptime |
| `/api/led/on` และ `/api/led/off` | JSON + พิมพ์ log ที่ terminal |
| `/api/student` | JSON ข้อมูลนักศึกษา (Micro-Challenge) |

### Lab 8-2 — ESP32 Firmware (ESP-IDF v6.1 แบบ Native — ไม่ใช้ Docker)

ใช้ ESP-IDF v6.1 ที่ติดตั้งไว้แล้วผ่าน **ESP-IDF Installation Manager (EIM)**
มีสคริปต์ช่วยเตรียมไว้ให้ในโฟลเดอร์โปรเจกต์แล้ว ไม่ต้องพิมพ์คำสั่งยาวๆ เอง

```powershell
cd Code\Lab8-2\ESP32_ADC_Stream
.\build.ps1
```

```powershell
.\flash.ps1 -Monitor
```

| สคริปต์ | หน้าที่ |
| :-- | :-- |
| `.\build.ps1` | เปิดใช้งาน ESP-IDF → `set-target esp32` → `build` |
| `.\build.ps1 -Target esp32c6` | เปลี่ยนชิปเป้าหมายเป็น ESP32-C6 |
| `.\build.ps1 -Clean` | `fullclean` ก่อน build ใหม่ |
| `.\flash.ps1` | flash ผ่านพอร์ตค่าเริ่มต้น **COM3** |
| `.\flash.ps1 -Monitor` | flash แล้วเปิด monitor ต่อทันที |
| `.\flash.ps1 -Port COM5` | ระบุพอร์ตอื่น |
| `.\flash.ps1 -Auto -Monitor` | ให้ค้นหาพอร์ตอัตโนมัติ |

> ⚠️ **ไฟล์ `.ps1` ทุกไฟล์ต้องบันทึกเป็น UTF-8 with BOM**
> Windows PowerShell 5.1 อ่านไฟล์ที่ไม่มี BOM เป็นรหัส ANSI ทำให้ตัวอักษรไทยในคอมเมนต์เพี้ยน
> จนขึ้น error `The string is missing the terminator` ทั้งที่โค้ดถูกต้อง
> ใน VS Code ดูมุมขวาล่างต้องเป็น **UTF-8 with BOM** (ไม่ใช่ UTF-8 เฉยๆ)

**ถ้าอยากพิมพ์คำสั่งเองแบบไม่ใช้สคริปต์** ให้เปิดใช้งานสภาพแวดล้อมก่อนทุกครั้งที่เปิด terminal ใหม่:

```powershell
. "C:\Espressif\tools\Microsoft.v6.1.PowerShell_profile.ps1"
```

แล้วจึงใช้ `idf.py set-target esp32`, `idf.py build`, `idf.py -p COM24 flash monitor` ได้ตามปกติ

> **ทำไมต้องเปิดใช้งานก่อน?** เครื่องนี้ไม่มี Python ติดตั้งแบบทั่วไป — ESP-IDF ใช้ Python
> ส่วนตัวที่อยู่ใน `C:\Espressif\tools\python\v6.1\venv` สคริปต์นี้จะเติม PATH และตัวแปร
> `IDF_PATH` / `IDF_TOOLS_PATH` ให้ครบ ถ้าไม่เรียกก่อนจะขึ้น error ว่าหา `idf.py` หรือ `python` ไม่เจอ

> ⚠️ ก่อนไปทำใบงาน 8-3 ต้องกด `Ctrl + ]` ออกจาก monitor เพื่อ**ปลดล็อกพอร์ต COM**
> ไม่เช่นนั้นฝั่ง C# จะเปิดพอร์ตไม่ได้และตกไปอยู่ Simulation Mode

### Lab 8-3 — Serial Bridge

```bash
dotnet run --project Code/Lab8-3/Kestrel_Serial_Gateway
```

เปิด `http://localhost:<port>/api/telemetry` แล้วหมุน Potentiometer + กด F5

### Lab 8-4 — SVG Dashboard

```bash
dotnet run --project Code/Lab8-4/Kestrel_SVG_Dashboard
```

เปิด `http://localhost:5062` (หน้าเว็บมาจาก `wwwroot/index.html`) แล้วหมุน Potentiometer
เข็มจะกวาดตามมือทันทีโดย**ไม่ต้อง refresh**

### Lab 8-5 — Dual-Channel Command Center

```bash
dotnet run --project Code\Lab8-5\Kestrel_Dual_Dashboard
```

เปิด `http://localhost:5017` — เกจ์ซ้าย (7-Segment) ตามมือหมุนจริง เกจ์ขวา (Speedometer)
ขยับตามสัญญาณจำลองช่อง B รายละเอียดครบอยู่ใน [Lab8-5/REPORT.md](Lab8-5/REPORT.md)

---

## การตั้งค่าพอร์ต COM (ใบงาน 8-3 และ 8-4)

ใบงานต้นฉบับฮาร์ดโค้ดพอร์ตไว้ในโค้ดเป็น `"COM24"` ซึ่งต้องแก้ไฟล์ `.cs` ทุกครั้งที่เปลี่ยนเครื่อง
ในโปรเจกต์นี้ย้ายมาอ่านจาก `appsettings.json` แทน:

```json
"SerialPort": {
  "Port": "COM24"
}
```

- **ใส่ชื่อพอร์ต** เช่น `"COM24"` → บังคับใช้พอร์ตนั้น (ถ้าไม่พบจะเตือนแล้ว fallback ไป auto)
- **เว้นว่าง `""`** → ให้ระบบเลือกพอร์ตแรกที่ไม่ใช่ `COM1` ให้อัตโนมัติ

ตรวจชื่อพอร์ตได้ด้วย:

```powershell
[System.IO.Ports.SerialPort]::GetPortNames()
```

---

## สถานะการตรวจสอบ

| โปรเจกต์ | ผลการตรวจสอบ | รันจริง |
| :-- | :-- | :-- |
| `Lab8-1/Kestrel_API` | ✅ `dotnet build` — 0 errors, 0 warnings | ต้องรันเองใน terminal |
| `Lab8-3/Kestrel_Serial_Gateway` | ✅ `dotnet build` — 0 errors, 0 warnings | ต้องรันเองใน terminal |
| `Lab8-4/Kestrel_SVG_Dashboard` | ✅ `dotnet build` — 0 errors, 0 warnings | ✅ ทดสอบบนเบราว์เซอร์แล้ว |
| `Lab8-5/Kestrel_Dual_Dashboard` | ✅ `dotnet build` — 0 errors, 0 warnings | ✅ ทดสอบบนเบราว์เซอร์แล้ว |
| `Lab8-2/ESP32_ADC_Stream` | ✅ `idf.py build` — **Project build complete** (ESP-IDF v6.1, target esp32) | flash ลงบอร์ดจริงได้เลย |

ผลลัพธ์ของ Lab8-2:

```text
Generated ...\build\ESP32_ADC_Stream.bin
ESP32_ADC_Stream.bin binary size 0x24ae0 bytes.
Smallest app partition is 0x100000 bytes. 0xdb520 bytes (86%) free.
Project build complete.
```

### รายละเอียดการตรวจ API ของ Lab8-2 (เทียบกับ header ใน `C:\esp\v6.1\esp-idf`)

| สิ่งที่ใช้ในโค้ด | ผลการตรวจ |
| :-- | :-- |
| คอมโพเนนต์ `esp_adc` | ✅ มีอยู่ที่ `components/esp_adc` (จึงต้องใส่ `REQUIRES esp_adc`) |
| `adc_oneshot_new_unit(const cfg*, handle*)` | ✅ ตรงกับ signature จริง |
| `adc_oneshot_config_channel(handle, channel, const cfg*)` | ✅ ตรงกับ signature จริง |
| `adc_oneshot_read(handle, chan, int *out_raw)` | ✅ คืน `esp_err_t` ส่วนค่าที่อ่านได้เป็น `int` ผ่าน out param |
| `adc_oneshot_unit_init_cfg_t { unit_id, ulp_mode }` | ✅ ฟิลด์มีจริง |
| `adc_oneshot_chan_cfg_t { atten, bitwidth }` | ✅ ฟิลด์มีจริง |
| `ADC_ATTEN_DB_12` | ✅ มีใน v6.1 (ชื่อเดิม `ADC_ATTEN_DB_11` ถูกยกเลิกไปแล้ว) |
| `ADC_BITWIDTH_12` / `ADC_ULP_MODE_DISABLE` | ✅ มีจริงทั้งคู่ |
| `ADC_CHANNEL_4` = **GPIO 32** (ESP32) | ✅ `ADC1_CHANNEL_4_GPIO_NUM 32` |
| `ADC_CHANNEL_4` = GPIO 4 (ESP32-C6) | ✅ `ADC1_CHANNEL_4_GPIO_NUM 4` |

---

## สิ่งที่ยังต้องทำก่อนส่งงาน

- [ ] แก้ `studentName` และ `faculty` ใน `Lab8-1/Kestrel_API/Program.cs` (ยังเป็น `TODO:`)
- [ ] ตั้งค่า `SerialPort:Port` ใน `appsettings.json` ของ Lab8-3 และ Lab8-4 ให้ตรงกับเครื่อง
- [ ] Build และ flash เฟิร์มแวร์ Lab8-2 ลงบอร์ด ESP32
- [ ] เลือกทำ Creative Widget อย่างน้อย 1 อย่างในใบงาน 8-4 (VU Meter / 7-Segment / Liquid Tank)
- [ ] เก็บภาพหน้าจอและวิดีโอตามที่ระบุใน [../Answer/ANSWERS.md](../Answer/ANSWERS.md)
- [ ] commit แยกทีละใบงาน
