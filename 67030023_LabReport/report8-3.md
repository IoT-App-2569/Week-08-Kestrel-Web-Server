## reportLab8-3

#### 📋 [Checkpoint 3.1: ทดสอบกลไก Fallback Simulation]
1. ขณะที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์
2. กด Refresh (F5) บนหน้าเบราว์เซอร์หลายๆ ครั้งติดต่อกัน สังเกตว่า:
   - ค่า `dataSource` เปลี่ยนเป็นอะไร?
   - ค่า `rawValue` ยังเปลี่ยนได้อยู่หรือไม่ และเปลี่ยนในลักษณะใด?
   - **คำตอบ:** ....................................................................................................
### ผลการสังเกตและบันทึกคำตอบ
- **ค่า `dataSource` เปลี่ยนเป็นอะไร?**
  - **คำตอบ:** เปลี่ยนจาก `"ESP32_Hardware"` (หรือ `"SerialPort"`) ไปเป็น **`"Simulation_Fallback"`**
- **ค่า `rawValue` ยังเปลี่ยนได้อยู่หรือไม่ และเปลี่ยนในลักษณะใด?**
  - **คำตอบ:** **ยังคงเปลี่ยนได้อยู่** โดยเปลี่ยนในลักษณะ**สุ่มตัวเลขแบบสุ่มจำลอง (Randomized Simulation)** ในช่วงค่า ADC (เช่น สุ่มระหว่าง 0 ถึง 4095) ทุกครั้งที่มีการ Refresh หรือส่ง HTTP Request เข้ามาใหม่
- **สรุปเหตุผลประกอบ:**
  - **คำตอบ:** เนื่องจากระบบมีกลไก Exception Handling (Try-Catch) ที่ตรวจจับสภาวะการหลุดเชื่อมต่อของฮาร์ดแวร์ เมื่อไม่สามารถสื่อสารผ่าน Serial Port ของ ESP32 ได้ ระบบจะสลับไปใช้ฟังก์ชันจำลองข้อมูล (Fallback Mode) อัตโนมัติ เพื่อให้ Minimal API ยังคงส่งคืนข้อมูล JSON ตอบกลับผู้ใช้งานได้โดยที่แอปพลิเคชันไม่พัง (Crash)
---

## 🎯 ภารกิจท้าทาย (Micro-Challenge)
- ให้นักศึกษาเพิ่มฟิลด์ `alertLevel` เข้าไปในผลลัพธ์ JSON ของ `/api/telemetry`:
app.MapGet("/api/telemetry", () => {
    // สมมติการอ่านค่า หรือรับค่า percentage (0.0 - 100.0)
    double rawValue = GetSensorRawValue(); // ฟังก์ชันอ่านค่า ADC
    double percentage = (rawValue / 4095.0) * 100.0;

    // เงื่อนไขกำหนดระดับการแจ้งเตือน alertLevel
    string alertLevel;
    if (percentage > 85.0) {
        alertLevel = "DANGER (HIGH)";
    } else if (percentage >= 70.0) {
        alertLevel = "WARNING";
    } else {
        alertLevel = "NORMAL";
    }

    return Results.Ok(new {
        sensor = "LDR_Sensor",
        rawValue = rawValue,
        percentage = Math.Round(percentage, 2),
        alertLevel = alertLevel,
        dataSource = IsEsp32Connected ? "ESP32_Hardware" : "Simulation_Fallback",
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    });
});
  - ถ้า `percentage` มากกว่า 85.0% ให้ส่งค่า `"DANGER (HIGH)"`
  {
  "sensor": "LDR_Sensor",
  "rawValue": 3920.0,
  "percentage": 95.73,
  "alertLevel": "DANGER (HIGH)",
  "dataSource": "ESP32_Hardware",
  "timestamp": "2026-10-29 15:22:05"
}
  - ถ้า `percentage` ระหว่าง 70.0% - 85.0% ให้ส่งค่า `"WARNING"`
  {
  "sensor": "LDR_Sensor",
  "rawValue": 3100.0,
  "percentage": 75.70,
  "alertLevel": "WARNING",
  "dataSource": "ESP32_Hardware",
  "timestamp": "2026-10-29 15:21:15"
}
  - ถ้า `percentage` ต่ำกว่า 70.0% ให้ส่งค่า `"NORMAL"`
  {
  "sensor": "LDR_Sensor",
  "rawValue": 2450.0,
  "percentage": 59.83,
  "alertLevel": "NORMAL",
  "dataSource": "ESP32_Hardware",
  "timestamp": "2026-10-29 15:20:10"
}
- ### 3. หลักฐานการทดสอบฟิลด์ alertLevel บนเบราว์เซอร์

- **สภาวะ NORMAL (< 70.0%):**
  ![NORMAL Alert](./alert-normal.png)

- **สภาวะ WARNING (70.0% - 85.0%):**
  ![WARNING Alert](./alert-warning.png)

- **สภาวะ DANGER (HIGH) (> 85.0%):**
  ![DANGER Alert](./alert-danger.png)