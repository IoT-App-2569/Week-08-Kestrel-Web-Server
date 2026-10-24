# รายงานผลการทดลอง ใบงานที่ 08-5

## ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์ (Dual-Channel IoT Command Center)

| หัวข้อ       | รายละเอียด                          |
| ------------ | ----------------------------------- |
| ชื่อ-นามสกุล | เทพทัต บัวเทศ                       |
| รหัสนักศึกษา | **67030296**                        |
| บอร์ด        | ESP32-WROOM-32 (NodeMCU-32S, 38 ขา) |
| พอร์ต Serial | COM5 (Silicon Labs CP210x)          |
| โปรเจกต์     | `Lab8-5/Kestrel_Dual_Dashboard`     |

---

## 1. การคำนวณหมายเลขเกจ์จากรหัสนักศึกษา

กำหนดให้ **N = 296**

|ตำแหน่ง|สูตร|การคำนวณ|ผล|
|---|---|---|:-:|
|เกจ์ซ้าย (Left)|(N mod 4) + 1|296 mod 4 = 0 → 0 + 1|**1**|
|เกจ์ขวา (Right)|(⌊N / 4⌋ mod 4) + 1|⌊296 / 4⌋ = 74 → 74 mod 4 = 2 → 2 + 1|**3**|

- Left = 1 และ Right = 3 ไม่ซ้ำกัน จึง**ไม่ต้องใช้กฎ Anti-Collision**
- **เกจ์ซ้าย = 1 → Analog Speedometer** (เข็มไมล์กวาดมุม −90° ถึง +90°) แสดงค่าช่อง A
- **เกจ์ขวา = 3 → Retro 7-Segment** (ตัวเลขดิจิทัล 2 หลัก 00–99%) แสดงค่าช่อง B

---

## 2. แหล่งข้อมูล 2 ช่องสัญญาณ

|ช่อง|แหล่งข้อมูล|การต่อวงจร / วิธีสร้าง|
|---|---|---|
|**Channel A**|Potentiometer ฮาร์ดแวร์จริง|ขาริม → 3V3, ขากลาง → GPIO34 (ADC1_CH6), ขาริม → GND · ESP32 ส่งค่า `printf("%d\n", raw_val)` ทาง USB 115200 bps|
|**Channel B**|สัญญาณจำลอง (ทางเลือกที่ 1)|C# Background Worker สร้าง Sine Wave: `(sin(t × 1.2) + 1) / 2 × 4095`|

ตัวอย่าง JSON ที่ได้จาก `GET /api/telemetry` ขณะทดลองจริง:

```json
{
  "channelA": {
    "name": "Potentiometer (Hardware)",
    "rawValue": 4095,
    "voltage": 3.3,
    "percentage": 100
  },
  "channelB": {
    "name": "Room Sensor (Simulated)",
    "rawValue": 267,
    "voltage": 0.22,
    "percentage": 6.5
  },
  "dataSource": "Live ESP32 (COM5) + Sim B",
  "lastUpdated": "2026-10-24T13:51:46.099Z"
}
```

---

## 3. ภาพหน้าจอแดชบอร์ด

[Dual-Channel Dashboard](https://claude.ai/chat/Images/Lab8-5-dashboard.png)

_ภาพที่ 1 แดชบอร์ดขณะทำงาน: ซ้ายเป็น Speedometer (Channel A จาก Potentiometer), ขวาเป็น 7-Segment (Channel B จำลอง), ด้านบนแสดงรหัส N = 296 และผลการคำนวณเกจ์, ด้านล่างแสดงแหล่งข้อมูล `Live ESP32 (COM5) + Sim B`_

---

## 4. หลักการทำงานของฟังก์ชัน JavaScript

ภาพรวมการไหลของข้อมูล:

```
Potentiometer → ESP32 ADC → USB Serial (COM5) → C# Background Worker
→ DualChannelStateStore → GET /api/telemetry (JSON)
→ pollTelemetry() → updateLeftWidget() / updateRightWidget() → SVG บนหน้าเว็บ
```

### 4.1 `pollTelemetry()` — ดึงข้อมูลแบบ Polling

```javascript
async function pollTelemetry() {
    try {
        const res = await fetch('/api/telemetry');
        if (!res.ok) return;
        const data = await res.json();

        updateLeftWidget(data.channelA.percentage);   // เกจ์ซ้าย ← ช่อง A
        updateRightWidget(data.channelB.percentage);  // เกจ์ขวา ← ช่อง B
        document.getElementById('disp-source').textContent = '📡 ' + data.dataSource;
    } catch (err) {
        console.error('Polling error:', err);
    }
}
setInterval(pollTelemetry, 150);
```

- ใช้ `fetch()` ส่ง HTTP GET ไปที่ `/api/telemetry` บน Kestrel แบบ **asynchronous** (`async/await`) หน้าเว็บจึงไม่ค้างระหว่างรอข้อมูล
- แปลงผลลัพธ์เป็น object ด้วย `res.json()` แล้วแยกส่งค่า `channelA.percentage` ให้เกจ์ซ้าย และ `channelB.percentage` ให้เกจ์ขวา
- `setInterval(pollTelemetry, 150)` เรียกซ้ำทุก **150 ms (≈ 7 ครั้ง/วินาที)** ทำให้เกจ์ตอบสนองต่อการหมุนแทบทันที
- ถ้า server ไม่ตอบหรือเครือข่ายขัดข้อง `try/catch` จะดักข้อผิดพลาดไว้ หน้าเว็บไม่พัง และจะลองใหม่ในรอบถัดไปเอง

### 4.2 `percentToAngle()` และ `updateLeftWidget()` — หมุนเข็ม Speedometer

```javascript
function percentToAngle(pct) {
    return (pct / 100.0) * 180.0 - 90.0;
}

function updateLeftWidget(percentage) {
    const pct = Math.min(100, Math.max(0, percentage));
    document.getElementById('needle').style.transform = `rotate(${percentToAngle(pct)}deg)`;
    document.getElementById('val-left').textContent = pct.toFixed(1) + '%';
}
```

- ใช้ **Linear Mapping** แปลง 0–100% เป็นมุม −90° ถึง +90°: 0% → −90° (ชี้ซ้าย), 50% → 0° (ชี้ขึ้น), 100% → +90° (ชี้ขวา)
- จำกัดค่าให้อยู่ในช่วง 0–100 ด้วย `Math.min/Math.max` กันเข็มหมุนเกินหน้าปัด
- หมุนกลุ่ม `<g id="needle">` ด้วย CSS `transform: rotate()` โดยตั้ง `transform-origin: 150px 150px` ให้หมุนรอบแกนกลางหน้าปัด และใช้ `transition` 0.12 s ให้เข็มกวาดนุ่มนวล ไม่กระตุก

### 4.3 `setDigit()` และ `updateRightWidget()` — แสดงผล 7-Segment

```javascript
const SEGMENT_MAP = {
    0: [1,1,1,1,1,1,0], 1: [0,1,1,0,0,0,0], 2: [1,1,0,1,1,0,1],
    3: [1,1,1,1,0,0,1], 4: [0,1,1,0,0,1,1], 5: [1,0,1,1,0,1,1],
    6: [1,0,1,1,1,1,1], 7: [1,1,1,0,0,0,0], 8: [1,1,1,1,1,1,1],
    9: [1,1,1,1,0,1,1]
};
const SEG_NAMES = ['a','b','c','d','e','f','g'];

function setDigit(digitGroupId, num) {
    const group = document.getElementById(digitGroupId);
    const pattern = SEGMENT_MAP[num];
    SEG_NAMES.forEach((segName, index) => {
        group.querySelector(`.${segName}`).classList.toggle('active', pattern[index] === 1);
    });
}

function updateRightWidget(percentage) {
    const val = Math.min(99, Math.max(0, Math.round(percentage)));
    setDigit('seg-digit1', Math.floor(val / 10)); // หลักสิบ
    setDigit('seg-digit2', val % 10);             // หลักหน่วย
}
```

- `SEGMENT_MAP` เป็น **Truth Table** บอกว่าตัวเลข 0–9 แต่ละตัวต้องเปิด segment ไหนบ้างจาก a–g (1 = ติด, 0 = ดับ)
- `updateRightWidget()` ปัดเปอร์เซ็นต์เป็นจำนวนเต็มและจำกัดไว้ที่ 0–99 (จอมี 2 หลัก) แล้วแยกเป็น**หลักสิบ** (`Math.floor(val / 10)`) และ**หลักหน่วย** (`val % 10`)
- `setDigit()` วนทีละ segment แล้วใช้ `classList.toggle('active', ...)` เพิ่มหรือลบคลาส `active` ตาม Truth Table คลาสนี้เปลี่ยนสีเป็นนีออนเขียวพร้อม `drop-shadow` (Glow Effect) ส่วน segment ที่ดับจะจางลงเหลือ opacity 0.15

---

## 5. ปัญหาที่พบและการแก้ไข

|ปัญหา|สาเหตุ|การแก้ไข|
|---|---|---|
|ค่าไม่เข้ามาเลย (`rawValue` = 0, `dataSource` ค้างที่ "Initializing")|โค้ดต้นฉบับเลือกพอร์ตแรกที่ไม่ใช่ COM1 อัตโนมัติ ทำให้ไปเชื่อมต่อ COM6 ซึ่งเป็นพอร์ต Bluetooth ไม่ใช่ ESP32|ตรวจด้วย `Get-CimInstance Win32_PnPEntity` พบว่า ESP32 อยู่ที่ COM5 (CP210x) จึงกำหนด `targetPort = "COM5"`|
|ถอดสาย USB แล้วระบบไม่เข้าโหมดจำลอง|เมื่อไม่พบ COM5 โค้ดไปเปิดพอร์ต Bluetooth แทน ระบบจึงคิดว่าเชื่อมต่อสำเร็จแต่ไม่มีข้อมูลเข้ามา|แก้ให้เข้าโหมดจำลองทันทีเมื่อไม่พบพอร์ตที่ระบุ (`portReady`)|
|ค่า ADC ค้างที่ 4095 หรือ 0|สายของ Potentiometer เสียบไม่แน่น|กดหัวสายให้แน่น และตรวจตำแหน่งขา 3V3 / GPIO34 / GND กับตัวหนังสือบนบอร์ด|
|พอร์ต COM5 ถูกล็อก|ESP-IDF Monitor เปิดค้างไว้|ปิด Monitor (`Ctrl + ]`) ก่อน `dotnet run`|

---

## 6. สรุปผลการทดลอง

ระบบ Dual-Channel IoT Command Center ทำงานได้ครบตามโจทย์:

1. **เกจ์ตรงตามรหัส:** N = 296 ได้เกจ์ซ้าย = Speedometer และเกจ์ขวา = 7-Segment
2. **ตอบสนองแบบเรียลไทม์:** เมื่อหมุน Potentiometer เข็ม Speedometer กวาดตามทันที เพราะ polling ทุก 150 ms และใช้ CSS transition ให้เคลื่อนไหวนุ่มนวล
3. **ข้อมูล 2 ช่อง:** Kestrel ส่ง JSON `channelA` / `channelB` ถูกต้อง โดย A มาจากฮาร์ดแวร์จริง ส่วน B เป็นคลื่นไซน์จำลอง
4. **Responsive:** ใช้ CSS Grid `repeat(auto-fit, minmax(300px, 1fr))` จอกว้างแสดง 2 คอลัมน์ จอแคบหรือมือถือเรียงเป็นแนวตั้งอัตโนมัติ พร้อม Glow Effect ทั้งเข็มและ 7-Segment
5. **Fallback:** เมื่อถอดสาย USB ระบบสลับเป็น `Simulation Mode (2 Channels)` เอง และกลับเป็น Live เมื่อเสียบสายคืน

---

## 7. ไฟล์ที่เกี่ยวข้อง

- `Lab8-5/Kestrel_Dual_Dashboard/Program.cs` — Kestrel Server, `DualChannelStateStore`, `DualSerialBridgeWorker`
- `Lab8-5/Kestrel_Dual_Dashboard/wwwroot/index.html` — แดชบอร์ด SVG + JavaScript
- `Lab8-3/ESP32_ADC_Stream/main/ESP32_ADC_Stream.c` — โค้ด ESP32 อ่าน ADC GPIO34
- คลิปสาธิตการทำงาน:แนบอยู่ล่างไฟล์นี้