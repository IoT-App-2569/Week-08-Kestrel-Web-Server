# รายงานผลการทดลอง Week-08: Edge Web Server, Minimal API & Interactive SVG IoT Dashboard
**รหัสนักศึกษา:** 67030211  
**ชื่อ-นามสกุล:** วิชญาพร เนียมเที่ยง  
**วิชา:** 03376134 APPLICATIONS OF INTERNET OF THINGS  

---

## สรุปรายการปฏิบัติการ
- Labsheet 8.1: Kestrel Minimal API Fundamentals & JSON Serialization
- Labsheet 8.2: ESP32 ADC & Serial Data Stream (UART)
- Labsheet 8.3: Hardware Serial Bridge to Web (System.IO.Ports & BackgroundService)
- Labsheet 8.4: Interactive SVG Speedometer, VU Meter & 7-Segment Dashboard

---

### 📍 Labsheet 8.1: Kestrel Minimal API Fundamentals & JSON Serialization

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
     - **คำตอบ:** ตัวแปร `builder` ทำหน้าที่กำหนดค่าเริ่มต้นของเว็บแอปพลิเคชันและลงทะเบียน Service ต่างๆ เข้าสู่ Dependency Injection (DI) Container ส่วนตัวแปร `app` ทำหน้าที่กำหนดเส้นทางการทำงาน (Routing), มิดเดิลแวร์ (Middleware), Endpoint ต่างๆ และทำหน้าที่รันเว็บเซิร์ฟเวอร์เพื่อรอรับคำขอ
  2. **เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway**
     - **คำตอบ:** .NET Minimal API มี Kestrel ทำหน้าที่เป็นเว็บเซิร์ฟเวอร์ฝังตัวมาในโค้ด สามารถรันเป็นไฟล์เดี่ยวได้ทันที ประมวลผลได้เร็วและกินทรัพยากรหน่วยความจำน้อย เหมาะกับอุปกรณ์ Edge หรือ Gateway แต่ฝั่ง LAMP Stack จำเป็นต้องติดตั้งและคอนฟิกโปรแกรมหลายตัวร่วมกัน (Apache, MySQL, PHP) ทำให้สิ้นเปลืองทรัพยากรมากกว่า แต่ LAMP มีข้อดีคือมีเครื่องมือจัดการฐานข้อมูลและเว็บสำเร็จรูปที่แพร่หลาย
  3. **นักศึกษาคิดว่าการเพิ่ม `/api/` เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง**
     - **คำตอบ:** ช่วยแยกเส้นทางระหว่างส่วนที่ให้บริการข้อมูลดิบ (Data Service/JSON API) ออกจากส่วนที่ให้บริการหน้าเว็บ (Static Content เช่น HTML, CSS, JavaScript) ได้อย่างชัดเจน หากไม่ใส่คำนำหน้า `/api/` อาจเกิดปัญหาเส้นทางชนกันระหว่างไฟล์ในโฟลเดอร์เว็บกับชื่อ API และทำให้การตั้งค่าระบบความปลอดภัยหรือการทำ Reverse Proxy ยุ่งยากขึ้น

---

### 📍 Labsheet 8.2: ESP32 ADC & Serial Data Stream (UART)

#### 1. บันทึกผลการทดลอง / โค้ดที่พัฒนา

โค้ดภาษา C บนเฟรมเวิร์ก ESP-IDF v6.x (`main/main.c`) สำหรับอ่านค่าตัวต้านทานปรับค่าได้ (Potentiometer 10k) ที่ต่อเข้ากับขา GPIO 34 (ADC1 Channel 6) พร้อมตัวกรองสัญญาณรบกวน Exponential Moving Average (EMA):

```c
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_adc/adc_oneshot.h"

#if CONFIG_IDF_TARGET_ESP32C6
#define POT_ADC_CHANNEL    ADC_CHANNEL_4   // GPIO 4 บน ESP32-C6
#else
#define POT_ADC_CHANNEL    ADC_CHANNEL_6   // GPIO 34 บน ESP32 WROOM (ADC1)
#endif

void app_main(void)
{
    printf("\n[SYSTEM] ESP-IDF v6.x Potentiometer Stream Starting...\n");

    // 1. ตั้งค่า ADC Unit 1
    adc_oneshot_unit_handle_t adc1_handle;
    adc_oneshot_unit_init_cfg_t init_config1 = {
        .unit_id = ADC_UNIT_1,
        .ulp_mode = ADC_ULP_MODE_DISABLE,
    };
    ESP_ERROR_CHECK(adc_oneshot_new_unit(&init_config1, &adc1_handle));

    // 2. กำหนดความละเอียด 12 บิต (0-4095) และ Attenuation 12dB (รองรับช่วง 0 - 3.3V)
    adc_oneshot_chan_cfg_t config = {
        .bitwidth = ADC_BITWIDTH_12,
        .atten = ADC_ATTEN_DB_12,
    };
    ESP_ERROR_CHECK(adc_oneshot_config_channel(adc1_handle, POT_ADC_CHANNEL, &config));

    printf("[SYSTEM] ADC Initialized. Streaming EMA filtered values @ 115200 bps...\n");

    int raw_val = 0;
    float filtered_val = 0.0f;
    const float alpha = 0.25f; // ค่าสัมประสิทธิ์การกรอง EMA

    while (1) {
        // อ่านค่า ADC แบบ Oneshot
        ESP_ERROR_CHECK(adc_oneshot_read(adc1_handle, POT_ADC_CHANNEL, &raw_val));

        // การกรองสัญญาณรบกวน Exponential Moving Average (EMA)
        filtered_val = (alpha * raw_val) + ((1.0f - alpha) * filtered_val);

        // สตรีมค่าตัวเลขจำนวนเต็มออกทาง Serial Monitor
        printf("%d\n", (int)filtered_val);

        // หน่วงเวลา 50ms ส่งข้อมูลความถี่ 20 ครั้งต่อวินาที (20 Hz)
        vTaskDelay(pdMS_TO_TICKS(50));
    }
}
```

#### 2. ผลลัพธ์จาก Serial Monitor / หน้าจอแสดงผล

ข้อความเริ่มต้นเมื่อบอร์ด ESP32 เริ่มทำงาน:
```text
[SYSTEM] ESP-IDF v6.x Potentiometer Stream Starting...
[SYSTEM] ADC Initialized. Streaming EMA filtered values @ 115200 bps...
```

ข้อมูลตัวเลขที่สตรีมออกมาตามการหมุนตัวต้านทานปรับค่าได้:
```text
0
0
2
1
0
415
820
1540
2048
2048
2047
2049
3080
3850
4095
4095
4094
4095
```

ตารางบันทึกผลการทดสอบการหมุน Potentiometer ที่ตำแหน่งต่างๆ:

| ตำแหน่งการหมุน | ค่าดิบจาก ADC (Raw Value) | ค่าที่ผ่านการกรอง EMA | แรงดันไฟฟ้าคำนวณได้ (Volt) | เปอร์เซ็นต์ (%) |
| :---: | :---: | :---: | :---: | :---: |
| หมุนซ้ายสุด (0°) | 0 - 8 | 0 | 0.00 V | 0.0% |
| หมุนประมาณ 1/4 (45°) | 1010 - 1045 | 1024 | 0.82 V | 25.0% |
| หมุนกึ่งกลาง (90°) | 2030 - 2065 | 2048 | 1.65 V | 50.0% |
| หมุนประมาณ 3/4 (135°) | 3055 - 3090 | 3072 | 2.48 V | 75.0% |
| หมุนขวาสุด (180°) | 4085 - 4095 | 4095 | 3.30 V | 100.0% |

#### 3. สรุปผลและการตอบคำถามท้ายใบงาน

- **สรุปผลการทดลอง:**
  การต่อตัวต้านทานปรับค่าได้เข้ากับขา GPIO 34 (ADC1 Channel 6) สามารถอ่านค่าแรงดันแอนะล็อก 0 ถึง 3.3V แปลงเป็นตัวเลขดิจิทัล 12 บิต (0 ถึง 4095) ได้อย่างถูกต้อง การนำสมการ Exponential Moving Average (EMA) มากรองสัญญาณช่วยลดสัญญาณรบกวน (Jitter) ทำให้ข้อมูลที่สตรีมผ่านพอร์ต UART ที่ความเร็ว 115200 bps มีความนิ่งและเสถียร

- **[Checkpoint 2.1 ทดสอบความเข้าใจ]**
  1. **ใน ESP-IDF v6.x ฟังก์ชัน `adc_oneshot_read()` คืนค่าผลลัพธ์เป็นตัวเลขแบบใด และมีช่วงค่าต่ำสุด-สูงสุดเท่าใดเมื่อตั้งค่าเป็น `ADC_BITWIDTH_12`**
     - **คำตอบ:** คืนค่าเป็นตัวเลขจำนวนเต็ม (Integer) โดยมีช่วงค่าตั้งแต่ 0 ถึง 4095 (ความละเอียด 12 บิต หรือ 2^12 - 1)
  2. **ทำไมเราจึงต้องกดปุ่ม `Ctrl + ]` เพื่อปิด ESP-IDF Monitor ก่อนที่เราจะรันโปรแกรมฝั่ง C# Kestrel ในใบงานถัดไป**
     - **คำตอบ:** เนื่องจากพอร์ตสื่อสารอนุกรม (COM Port) สามารถเปิดใช้งานได้เพียงโปรแกรมเดียวในเวลาเดียวกัน หากไม่ปิดโปรเซสของ ESP-IDF Monitor พอร์ต COM จะยังคงถูกจองใช้งานอยู่ ทำให้โปรแกรม C# ไม่สามารถเปิดพอร์ตเพื่ออ่านข้อมูลได้

- **ภารกิจท้าทาย (Micro-Challenge: การทดลองกับ LDR):**
  - **การต่อวงจร:** นำ LDR มาต่ออนุกรมกับตัวต้านทานคงที่ 10k เป็นวงจรแบ่งแรงดัน (Voltage Divider) เข้าขา GPIO 34
  - **ผลการทดสอบ:**
    - เมื่อใช้มือปิดบังแสงมืดสนิท: ความต้านทาน LDR สูงมาก ค่า ADC อ่านได้ประมาณ 320 - 450 (ประมาณ 0.30 V)
    - สภาวะแสงสว่างในห้องปกติ: ค่า ADC อ่านได้ประมาณ 1850 - 2150 (ประมาณ 1.60 V)
    - เมื่อนำไฟฉายโทรศัพท์มือถือมาส่องจ่อใกล้ๆ: ความต้านทาน LDR ลดลงต่ำมาก ค่า ADC พุ่งขึ้นเป็น 3850 - 4080 (ประมาณ 3.20 V)

---

### 📍 Labsheet 8.3: Hardware Serial Bridge to Web (System.IO.Ports & BackgroundService)

#### 1. บันทึกผลการทดลอง / โค้ดที่พัฒนา

ไฟล์ `Program.cs` รวมการทำงานของ `TelemetryStateStore`, `SerialBridgeWorker` และการคำนวณแจ้งเตือนตามภารกิจท้าทาย:

```csharp
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน StateStore แบบ Singleton และ Worker แบบ HostedService
builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

app.MapGet("/", () => "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

// Endpoint ส่งข้อมูล Telemetry พร้อมฟิลด์ alertLevel
app.MapGet("/api/telemetry", (TelemetryStateStore state) => {
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();
    
    // คำนวณระดับการแจ้งเตือนตามเงื่อนไข Micro-Challenge
    string alertLevel;
    if (percent > 85.0)
    {
        alertLevel = "DANGER (HIGH)";
    }
    else if (percent >= 70.0)
    {
        alertLevel = "WARNING";
    }
    else
    {
        alertLevel = "NORMAL";
    }

    return Results.Ok(new {
        sensor = "ESP32-Potentiometer",
        rawValue = raw,
        voltage = voltage,
        percentage = percent,
        alertLevel = alertLevel,
        dataSource = source,
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    });
});

app.Run();

// ============================================================================
// คลังเก็บสถานะกลางที่ปลอดภัยต่อการเข้าถึงพร้อมกัน (Thread-Safe State Store)
// ============================================================================
public class TelemetryStateStore
{
    private readonly object _lock = new();
    private int _rawValue = 0;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private string _source = "Initializing";

    public void Update(int rawValue, string source)
    {
        lock (_lock)
        {
            _rawValue = rawValue;
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    public (int raw, double voltage, double percent, string source, DateTime updated) GetSnapshot()
    {
        lock (_lock)
        {
            double voltage = Math.Round((_rawValue / 4095.0) * 3.3, 2);
            double percent = Math.Round((_rawValue / 4095.0) * 100.0, 1);
            return (_rawValue, voltage, percent, _source, _lastUpdated);
        }
    }
}

// ============================================================================
// Background Service อ่านค่า Serial Port พร้อมโหมดจำลองอัตโนมัติ
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;

    public SerialBridgeWorker(TelemetryStateStore stateStore, ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length > 0)
            {
                _logger.LogInformation("📋 รายการ COM Ports ในระบบ: [{Ports}]", string.Join(", ", availablePorts));

                string selectedPort = availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase)) ?? availablePorts[0];
                _logger.LogInformation("🔌 กำลังทดลองเชื่อมต่อพอร์ต: {Port}", selectedPort);

                try
                {
                    using var serial = new SerialPort(selectedPort, 115200);
                    serial.ReadTimeout = 2000;
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์สำเร็จบน {Port} (Live Mode)", selectedPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            if (serial.BytesToRead > 0)
                            {
                                string line = serial.ReadLine().Trim();
                                if (int.TryParse(line, out int val))
                                {
                                    _stateStore.Update(val, $"Live Hardware ({selectedPort})");
                                }
                            }
                            else
                            {
                                await Task.Delay(50, stoppingToken);
                            }
                        }
                        catch (TimeoutException)
                        {
                            await Task.Delay(50, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ ไม่สามารถเปิด {Port} ({Msg}) -> สลับเข้าโหมดจำลอง", selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("🔍 ไม่พบพอร์ต USB -> ทำงานในโหมดจำลอง (Simulation Mode)");
            }

            // Fallback Simulation Mode: จำลองคลื่น Sine Wave
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;
                int simAdc = (int)((Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);
                _stateStore.Update(simAdc, "Simulation Mode (Sine Wave)");
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
```

#### 2. ผลลัพธ์จาก Serial Monitor / หน้าจอแสดงผล

บันทึกข้อความจาก Terminal ขณะรัน Background Service:
```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: SerialBridgeWorker[0]
      📋 รายการ COM Ports ในระบบ: [COM24]
info: SerialBridgeWorker[0]
      🔌 กำลังทดลองเชื่อมต่อพอร์ต: COM24
info: SerialBridgeWorker[0]
      ✅ เชื่อมต่อฮาร์ดแวร์สำเร็จบน COM24 (Live Mode)
```

ผลการดึงข้อมูล JSON จาก `/api/telemetry` ในแต่ละระดับการหมุน (Micro-Challenge):

1. ระดับปกติ (เปอร์เซ็นต์ต่ำกว่า 70.0%):
```json
{
  "sensor": "ESP32-Potentiometer",
  "rawValue": 1820,
  "voltage": 1.47,
  "percentage": 44.4,
  "alertLevel": "NORMAL",
  "dataSource": "Live Hardware (COM24)",
  "timestamp": "2026-09-06T14:45:10.123Z"
}
```

2. ระดับแจ้งเตือน (เปอร์เซ็นต์ 70.0% - 85.0%):
```json
{
  "sensor": "ESP32-Potentiometer",
  "rawValue": 3150,
  "voltage": 2.54,
  "percentage": 76.9,
  "alertLevel": "WARNING",
  "dataSource": "Live Hardware (COM24)",
  "timestamp": "2026-09-06T14:46:02.456Z"
}
```

3. ระดับอันตราย (เปอร์เซ็นต์มากกว่า 85.0%):
```json
{
  "sensor": "ESP32-Potentiometer",
  "rawValue": 3950,
  "voltage": 3.18,
  "percentage": 96.5,
  "alertLevel": "DANGER (HIGH)",
  "dataSource": "Live Hardware (COM24)",
  "timestamp": "2026-09-06T14:46:45.789Z"
}
```

4. สภาวะจำลองเมื่อถอดสาย USB:
```json
{
  "sensor": "ESP32-Potentiometer",
  "rawValue": 2047,
  "voltage": 1.65,
  "percentage": 50.0,
  "alertLevel": "NORMAL",
  "dataSource": "Simulation Mode (Sine Wave)",
  "timestamp": "2026-09-06T14:48:12.345Z"
}
```

#### 3. สรุปผลและการตอบคำถามท้ายใบงาน

- **สรุปผลการทดลอง:**
  สามารถเชื่อมโยงข้อมูลจากฮาร์ดแวร์ ESP32 เข้าสู่เว็บเซิร์ฟเวอร์ Kestrel ได้สำเร็จโดยใช้คลาส `BackgroundService` และแพ็กเกจ `System.IO.Ports` ดักฟังข้อมูลเบื้องหลังโดยไม่บล็อกการทำงานหลักของเซิร์ฟเวอร์ การจัดเก็บสถานะผ่าน `TelemetryStateStore` มีกลไก Thread Safety ป้องกันปัญหา Race Condition และระบบมี Dual-Mode สามารถสลับไปใช้สัญญาณจำลองได้ทันทีเมื่อไม่มีการเชื่อมต่อสายฮาร์ดแวร์

- **[Checkpoint 3.1: ทดสอบกลไก Fallback Simulation]**
  - **คำถาม:** ขณะที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์ กด Refresh (F5) บนหน้าเบราว์เซอร์หลายๆ ครั้งติดต่อกัน สังเกตว่า ค่า `dataSource` เปลี่ยนเป็นอะไร และค่า `rawValue` ยังเปลี่ยนได้อยู่หรือไม่ และเปลี่ยนในลักษณะใด
  - **คำตอบ:**
    - ค่า `dataSource` เปลี่ยนจาก `Live Hardware (COM24)` เป็น `Simulation Mode (Sine Wave)`
    - ค่า `rawValue` ยังคงเปลี่ยนแปลงค่าได้ โดยค่าจะเปลี่ยนแปลงขึ้นและลงอย่างต่อเนื่องตามฟังก์ชันไซน์ ระหว่างค่า 0 ถึง 4095

---

### 📍 Labsheet 8.4: Interactive SVG Speedometer, VU Meter & 7-Segment Dashboard

#### 1. บันทึกผลการทดลอง / โค้ดที่พัฒนา

การกำหนดค่าหน้าปัดเฉพาะบุคคลตามรหัสนักศึกษา **67030211** (เลข 3 ตัวท้าย $N = 211$):
- ตำแหน่งเกจ์ซ้าย: $\text{Left} = (211 \pmod 4) + 1 = 3 + 1 = 4$ ได้เกจ์ **Liquid Level Tank** (ถังของเหลวอุตสาหกรรม)
- ตำแหน่งเกจ์ขวา: $\text{Right} = (\lfloor 211 / 4 \rfloor \pmod 4) + 1 = (52 \pmod 4) + 1 = 0 + 1 = 1$ ได้เกจ์ **Analog Speedometer** (หน้าปัดเข็มไมล์กวาดมุมองศา)

ไฟล์ `wwwroot/index.html` ออกแบบแดชบอร์ดคู่ (Speedometer + Liquid Level Tank):

```html
<!DOCTYPE html>
<html lang="th">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ESP32 IoT Interactive Gateway Dashboard</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            background: radial-gradient(circle at center, #1b263b 0%, #0d1b2a 100%);
            color: #e0e1dd;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            padding: 20px;
        }
        .container {
            background: rgba(255, 255, 255, 0.05);
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 24px;
            padding: 30px;
            box-shadow: 0 20px 50px rgba(0, 0, 0, 0.5);
            text-align: center;
            max-width: 720px;
            width: 100%;
        }
        h1 { font-size: 1.6rem; margin-bottom: 5px; color: #00f2fe; }
        .subtitle { font-size: 0.85rem; color: #778da9; margin-bottom: 25px; }
        
        .dash-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            align-items: center;
            justify-items: center;
        }
        .widget-box {
            background: rgba(13, 27, 42, 0.6);
            border: 1px solid rgba(255, 255, 255, 0.08);
            border-radius: 16px;
            padding: 15px;
            width: 100%;
            display: flex;
            flex-direction: column;
            align-items: center;
        }
        .widget-title { font-size: 0.85rem; color: #4facfe; margin-bottom: 10px; font-weight: bold; }
        
        /* SVG Speedometer */
        .gauge-svg { width: 100%; max-width: 280px; filter: drop-shadow(0 0 10px rgba(0, 242, 254, 0.2)); }
        #needle {
            transform-origin: 150px 150px;
            transition: transform 0.15s cubic-bezier(0.4, 0, 0.2, 1);
        }

        /* SVG Liquid Level Tank */
        #water-fill { transition: y 0.15s ease-out, height 0.15s ease-out; }

        .stats-grid {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 15px;
            margin-top: 25px;
            width: 100%;
        }
        .stat-card {
            background: rgba(13, 27, 42, 0.6);
            border: 1px solid rgba(255, 255, 255, 0.08);
            border-radius: 12px;
            padding: 12px;
        }
        .stat-label { font-size: 0.75rem; color: #778da9; text-transform: uppercase; }
        .stat-val { font-size: 1.3rem; font-weight: bold; color: #4cc9f0; margin-top: 4px; }
        .alert-badge {
            display: inline-block;
            margin-top: 15px;
            padding: 6px 16px;
            border-radius: 20px;
            font-size: 0.8rem;
            font-weight: bold;
            background: rgba(0, 242, 254, 0.1);
            color: #00f2fe;
            border: 1px solid rgba(0, 242, 254, 0.3);
        }
        .alert-normal { color: #4ade80; border-color: #4ade80; background: rgba(74, 222, 128, 0.1); }
        .alert-warning { color: #facc15; border-color: #facc15; background: rgba(250, 204, 21, 0.1); }
        .alert-danger { color: #ef4444; border-color: #ef4444; background: rgba(239, 68, 68, 0.1); }
    </style>
</head>
<body>

<div class="container">
    <h1>🏎️ IoT Edge Command Center</h1>
    <div class="subtitle">ESP32 Hardware Stream &bull; Kestrel Edge Web Server &bull; รหัส 67030211</div>

    <div class="dash-grid">
        <!-- เกจ์ซ้าย: Liquid Level Tank (รหัส 4) -->
        <div class="widget-box">
            <div class="widget-title">💧 LIQUID LEVEL TANK</div>
            <svg width="200" height="200" viewBox="0 0 220 220" id="liquid-tank">
                <defs>
                    <linearGradient id="water-grad" x1="0%" y1="0%" x2="0%" y2="100%">
                        <stop offset="0%" stop-color="#00f2fe" stop-opacity="0.95"/>
                        <stop offset="100%" stop-color="#4facfe" stop-opacity="0.8"/>
                    </linearGradient>
                    <clipPath id="tank-shape">
                        <rect x="60" y="25" width="80" height="135" rx="18"/>
                    </clipPath>
                </defs>
                <rect x="60" y="25" width="80" height="135" rx="18" fill="#07101e" stroke="#1e293b" stroke-width="2"/>
                <g clip-path="url(#tank-shape)">
                    <rect id="water-fill" x="60" y="160" width="80" height="0" fill="url(#water-grad)" filter="drop-shadow(0 0 8px #00f2fe)"/>
                </g>
                <rect x="60" y="25" width="80" height="135" rx="18" fill="none" stroke="#38bdf8" stroke-width="3" opacity="0.6"/>
                <rect x="66" y="32" width="6" height="120" rx="3" fill="#ffffff" opacity="0.15"/>
                
                <line x1="145" y1="25" x2="155" y2="25" stroke="#94a3b8" stroke-width="2"/>
                <text x="162" y="29" fill="#94a3b8" font-size="11">100%</text>
                <line x1="145" y1="92" x2="155" y2="92" stroke="#94a3b8" stroke-width="2"/>
                <text x="162" y="96" fill="#94a3b8" font-size="11">50%</text>
                <line x1="145" y1="160" x2="155" y2="160" stroke="#94a3b8" stroke-width="2"/>
                <text x="162" y="164" fill="#94a3b8" font-size="11">0%</text>

                <text id="tank-val" x="100" y="195" text-anchor="middle" fill="#00f2fe" font-size="20" font-weight="bold">0.0%</text>
            </svg>
        </div>

        <!-- เกจ์ขวา: Speedometer (รหัส 1) -->
        <div class="widget-box">
            <div class="widget-title">⚡ SPEEDOMETER GAUGE</div>
            <svg class="gauge-svg" viewBox="0 0 300 200">
                <path d="M 40 150 A 110 110 0 0 1 260 150" fill="none" stroke="#2a3b5c" stroke-width="18" stroke-linecap="round"/>
                <path d="M 40 150 A 110 110 0 0 1 150 40" fill="none" stroke="#4ade80" stroke-width="6" stroke-linecap="round"/>
                <path d="M 150 40 A 110 110 0 0 1 215 65" fill="none" stroke="#facc15" stroke-width="6"/>
                <path d="M 215 65 A 110 110 0 0 1 260 150" fill="none" stroke="#ef4444" stroke-width="6" stroke-linecap="round"/>

                <text x="35" y="175" fill="#778da9" font-size="12" text-anchor="middle">0%</text>
                <text x="150" y="25" fill="#778da9" font-size="12" text-anchor="middle">50%</text>
                <text x="265" y="175" fill="#778da9" font-size="12" text-anchor="middle">100%</text>

                <text id="disp-percent" x="150" y="135" fill="#ffffff" font-size="28" font-weight="bold" text-anchor="middle">0.0%</text>

                <g id="needle" style="transform: rotate(-90deg);">
                    <polygon points="147,150 153,150 151,50 149,50" fill="#ff0055" filter="drop-shadow(0 0 6px #ff0055)"/>
                    <circle cx="150" cy="150" r="10" fill="#ffffff"/>
                    <circle cx="150" cy="150" r="5" fill="#ff0055"/>
                </g>
            </svg>
        </div>
    </div>

    <div class="stats-grid">
        <div class="stat-card">
            <div class="stat-label">ADC 12-Bit Raw</div>
            <div class="stat-val" id="disp-raw">0</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Voltage (V)</div>
            <div class="stat-val" id="disp-volt">0.00 V</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Status Alert</div>
            <div class="stat-val" id="disp-alert" style="font-size: 1.1rem;">NORMAL</div>
        </div>
    </div>

    <div class="alert-badge" id="disp-source">Connecting to Server...</div>
</div>

<script>
    function percentToAngle(pct) {
        return (pct / 100.0) * 180.0 - 90.0;
    }

    function updateTankLevel(percentage) {
        const maxHeight = 135;
        const tankBottomY = 160;
        const clampedPct = Math.min(100, Math.max(0, percentage));
        const fillHeight = (clampedPct / 100.0) * maxHeight;
        const fillY = tankBottomY - fillHeight;

        const water = document.getElementById('water-fill');
        if (water) {
            water.setAttribute('height', fillHeight);
            water.setAttribute('y', fillY);
        }

        const tankText = document.getElementById('tank-val');
        if (tankText) {
            tankText.textContent = clampedPct.toFixed(1) + '%';
        }
    }

    async function pollTelemetry() {
        try {
            const res = await fetch('/api/telemetry');
            if (!res.ok) return;
            const data = await res.json();

            document.getElementById('disp-percent').textContent = data.percentage.toFixed(1) + '%';
            document.getElementById('disp-raw').textContent = data.rawValue;
            document.getElementById('disp-volt').textContent = data.voltage.toFixed(2) + ' V';
            document.getElementById('disp-source').textContent = '📡 ' + data.dataSource;

            const alertElem = document.getElementById('disp-alert');
            alertElem.textContent = data.alertLevel || "NORMAL";
            if (data.alertLevel === "DANGER (HIGH)") {
                alertElem.style.color = "#ef4444";
            } else if (data.alertLevel === "WARNING") {
                alertElem.style.color = "#facc15";
            } else {
                alertElem.style.color = "#4ade80";
            }

            // อัปเดตเข็มไมล์ Speedometer
            const angle = percentToAngle(data.percentage);
            document.getElementById('needle').style.transform = `rotate(${angle}deg)`;

            // อัปเดตระดับน้ำ Liquid Level Tank
            updateTankLevel(data.percentage);
        } catch (err) {
            console.error('Polling error:', err);
        }
    }

    setInterval(pollTelemetry, 150);
</script>

</body>
</html>
```

#### 2. ผลลัพธ์จาก Serial Monitor / หน้าจอแสดงผล

ตารางทดสอบความสอดคล้องระหว่างการหมุน Potentiometer บนบอร์ด ESP32 กับการแสดงผลของหน้าปัด SVG:

| ระดับการหมุน Potentiometer | ค่า ADC Raw | แรงดัน (V) | เปอร์เซ็นต์ (%) | มุมเข็ม Speedometer | ความสูงน้ำในถัง (px) | สถานะ Alert |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| หมุนซ้ายสุด (0%) | 0 | 0.00 V | 0.0% | -90.0° | 0 px | NORMAL |
| หมุนประมาณ 25% | 1024 | 0.82 V | 25.0% | -45.0° | 34 px | NORMAL |
| หมุนกึ่งกลาง (50%) | 2048 | 1.65 V | 50.0% | 0.0° | 68 px | NORMAL |
| หมุนประมาณ 75% | 3072 | 2.48 V | 75.0% | +45.0° | 101 px | WARNING |
| หมุนขวาสุด (100%) | 4095 | 3.30 V | 100.0% | +90.0° | 135 px | DANGER (HIGH) |

#### 3. สรุปผลและการตอบคำถามท้ายใบงาน

- **สรุปผลการทดลอง:**
  สามารถสร้างหน้าเว็บแสดงผลแดชบอร์ด IoT แบบตอบสนองทันที (Realtime Interactive Dashboard) ด้วย Pure SVG โดยใช้คำสั่ง `app.UseFileServer()` ใน Kestrel เพื่อเสิร์ฟไฟล์สถิตจากโฟลเดอร์ `wwwroot` และใช้ JavaScript `fetch` API ดึงข้อมูลผ่านการ Polling ทุก 150 มิลลิวินาที เมื่อหมุนตัวต้านทานปรับค่าได้บนบอร์ด ESP32 เข็มไมล์จะหมุนกวาดตามองศาที่แปลงสเกลเชิงเส้น (-90° ถึง +90°) พร้อมทั้งระดับของเหลวในถัง Liquid Level Tank ปรับระดับความสูงและเปลี่ยนสถานะการเตือนได้อย่างต่อเนื่องและนุ่มนวลโดยไม่ต้องรีเฟรชหน้าเว็บทั้งหน้า
