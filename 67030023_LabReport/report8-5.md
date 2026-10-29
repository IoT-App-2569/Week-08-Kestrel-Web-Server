## reportLab8-5
---

## 1. ข้อมูลนักศึกษาและการคำนวณเลือกเกจ์ (Gauge Index Calculation)

จากการใช้รหัสนักศึกษา **67030023** คำนวณหาช่องข้อมูลและรูปแบบเกจ์ฝั่งซ้ายและฝั่งขวาตามเงื่อนไขของใบงาน:

### 1.1 เลขรหัสนักศึกษา
- **Student ID:** `67030023`
- **เลขท้าย 2 หลักสุดท้าย:** `23`

### 1.2 การคำนวณหาประเภทเกจ์ฝั่งซ้าย (Left Gauge - Hardware Sensor / Channel A)
$$\text{Left Gauge Index} = 23 \pmod 3 = 2$$
- **ผลการคำนวณ:** ได้ดัชนี **2** ➔ แสดงผลเป็น **Speedometer Gauge (เปอร์เซ็นต์ความสว่าง LDR)**

### 1.3 การคำนวณหาประเภทเกจ์ฝั่งขวา (Right Gauge - Simulation / Channel B)
$$\text{Right Gauge Index} = (23 + 1) \pmod 3 = 24 \pmod 3 = 0$$
- **ผลการคำนวณ:** ได้ดัชนี **0** ➔ แสดงผลเป็น **VU Meter / Level Bar Gauge (ค่าจำลองอุณหภูมิ/ความชื้น)**

---

## 2. โค้ด JavaScript ในการเชื่อมต่อ API และประมวลผล Dual-Gauge

```javascript
// ฟังก์ชันดึงข้อมูลแบบ Asynchronous จาก Kestrel Minimal API
async function fetchDualTelemetry() {
    try {
        const response = await fetch('/api/telemetry/dual');
        const data = await response.json();

        // 1. อัปเดตเกจ์ฝั่งซ้าย (Channel A: Hardware ESP32)
        updateLeftGauge(data.channelA.percentage, data.channelA.alertLevel);
        
        // 2. อัปเดตเกจ์ฝั่งขวา (Channel B: Simulation Data)
        updateRightGauge(data.channelB.value, data.channelB.status);

        // 3. แสดงสถานะบนหน้าจอ
        document.getElementById('studentIdDisplay').innerText = "Student ID: 67030023";
        document.getElementById('lastUpdate').innerText = data.timestamp;
    } catch (error) {
        console.error("Error fetching dual telemetry data:", error);
    }
}

+-----------------------------------------------------------------------+
|  Dual-Gauge Real-time Dashboard | Student ID: 67030023     -  □  ×    |
+-----------------------------------------------------------------------+
|  ←  →  ↻  | http://localhost:5186/dashboard/dual                      |
+-----------------------------------------------------------------------+
|                                                                       |
|                       🎓 Student ID: 67030023                         |
|                                                                       |
|   [ Left Gauge: Channel A ]               [ Right Gauge: Channel B ]  |
|      Hardware LDR Sensor                      Simulation Stream       |
|                                                                       |
|            /=======\                                 [████████░░]     |
|           /    |    \                                [████████░░]     |
|          |     /     |                               [████████░░]     |
|           \   /     /                                [████████░░]     |
|            \=======/                                                  |
|             75.70 %                                   82.50 °C        |
|          Status: WARNING                           Status: ACTIVE     |
|                                                                       |
|   Timestamp: 2026-10-29 15:35:20 | Source: ESP32 + Fallback Sim       |
+-----------------------------------------------------------------------+