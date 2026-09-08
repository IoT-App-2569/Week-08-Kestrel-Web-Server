# เฉลย/คำตอบ ใบงานที่ 8.5 — ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์

**รหัสนักศึกษา:** 67030098 &nbsp;|&nbsp; **ชื่อ:** Theeranat Phutiwanich &nbsp;|&nbsp; **Branch:** `67030098-HW`

| | |
| :-- | :-- |
| **ใบงานต้นฉบับ** | [10-Labsheet-08-5-Dual-Sensor-Command-Center.md](../10-Labsheet-08-5-Dual-Sensor-Command-Center.md) |
| **โค้ดของใบงานนี้** | [`Code/Lab8-5/Kestrel_Dual_Dashboard`](../Code/Lab8-5/Kestrel_Dual_Dashboard) |
| **รายงานฉบับเต็ม** | [`Code/Lab8-5/REPORT.md`](../Code/Lab8-5/REPORT.md) |
| **หัวข้อที่ตอบในไฟล์นี้** | การคำนวณคู่เกจ์ · Channel Mapping · โครงสร้าง JSON · หลักการ JavaScript · Rubric |

---

## 1. การคำนวณหน้าปัดเฉพาะบุคคล (Student-ID Gauge Assignment)

> **โจทย์:** นำเลขรหัสนักศึกษา 3 ตัวท้าย ($N$) มาคำนวณหาหมายเลขเกจ์ซ้ายและขวา

**คำตอบ:** รหัส **67030098** → เลข 3 ตัวท้าย $N = 098 = \mathbf{98}$

| ตำแหน่งเกจ์ | สูตร | การแทนค่าทีละขั้น | ผลลัพธ์ | ชนิดหน้าปัด |
| :-- | :-- | :-- | :---: | :-- |
| **เกจ์ซ้าย** | $(N \bmod 4) + 1$ | $98 \bmod 4 = 2$ &rarr; $2 + 1 = 3$ | **3** | **Retro 7-Segment** |
| **เกจ์ขวา** | $(\lfloor N/4 \rfloor \bmod 4) + 1$ | $\lfloor 98/4 \rfloor = 24$ &rarr; $24 \bmod 4 = 0$ &rarr; $0 + 1 = 1$ | **1** | **Analog Speedometer** |

**ตรวจสอบกฎป้องกันเกจ์ซ้ำ (Anti-Collision Rule):**
เกจ์ซ้าย = 3, เกจ์ขวา = 1 → **ไม่ซ้ำกัน** จึงไม่ต้องบวกเกจ์ขวาเพิ่ม 1

### ตารางเทียบหมายเลขเกจ์ (อ้างอิงจากใบงาน)

| หมายเลข | ชนิดของเกจ์ | ได้รับมอบหมายหรือไม่ |
| :---: | :-- | :-- |
| 1 | Analog Speedometer | ✅ **เกจ์ขวา** |
| 2 | Audio VU Meter | — |
| 3 | Retro 7-Segment | ✅ **เกจ์ซ้าย** |
| 4 | Liquid Level Tank | — |

> ผลการคำนวณนี้ถูกแสดงไว้บนหน้าเว็บด้วย (แถบด้านบนสุด) เพราะ Rubric ข้อ 1 กำหนดว่า
> คลิปวิดีโอต้องถ่ายให้เห็น**รหัสนักศึกษาและผลการคำนวณคู่เกจ์** — ทำแบบนี้ถ่ายหน้าจอเดียวได้ครบ

---

## 2. การจัดสรรช่องสัญญาณ (Dual-Channel Mapping)

| ช่อง | แหล่งข้อมูล | หน้าปัดที่แสดงผล | ตำแหน่ง |
| :-- | :-- | :-- | :-- |
| **Channel A** | Potentiometer B50K ฮาร์ดแวร์จริงบน ESP32 (**GPIO 32**) ผ่าน USB Serial @115200 | Retro 7-Segment | ซ้าย |
| **Channel B** | สัญญาณจำลอง Sine Wave สร้างจากฝั่ง C# (**ทางเลือกที่ 1** ตามใบงาน) **หรือ** ค่าที่ปรับเองจากสไลเดอร์บนหน้าเว็บ | Analog Speedometer | ขวา |

### โหมดการทำงานของช่อง B (Manual Override)

ช่อง B มี 2 โหมดให้สลับได้จากหน้าเว็บ ทำให้**ควบคุมเกจ์ขวาได้เองเหมือนหมุน Potentiometer**
ไม่ต้องนั่งรอให้คลื่นไซน์แกว่งไปถึงค่าที่อยากถ่ายภาพ

| โหมด | พฤติกรรม | เหมาะกับ |
| :-- | :-- | :-- |
| **🌊 Auto (Sine)** | แกว่งเป็นคลื่นไซน์เองอัตโนมัติ (ค่าเริ่มต้น ตรงตามใบงาน) | สาธิตว่าช่อง B ทำงานแยกจากช่อง A จริง |
| **🎚️ Manual** | ใช้ค่าจากสไลเดอร์ 0 – 4095 ที่ผู้ใช้เลื่อนเอง | ถ่ายภาพ/วิดีโอที่ต้องการค่าเจาะจง เช่น 0%, 50%, 100% |

**จุดออกแบบสำคัญ: เก็บค่าไว้ฝั่งเซิร์ฟเวอร์ ไม่ใช่ในหน้าเว็บ**
ตัวแปร `_manualB` และ `_manualBValue` อยู่ใน `DualChannelStateStore` (ป้องกันด้วย `lock` เหมือนข้อมูลอื่น)
ผลที่ได้คือ

1. เปิดหลายแท็บ/หลายเครื่องพร้อมกัน ทุกหน้าจอเห็นค่าตรงกันหมด
2. กด refresh แล้วค่าไม่หาย เพราะ state อยู่ที่เซิร์ฟเวอร์
3. ทดสอบผ่าน URL บนเบราว์เซอร์ตรงๆ ได้โดยไม่ต้องมีหน้าเว็บ

**เซิร์ฟเวอร์รองรับข้อมูลขาเข้า 3 กรณีโดยอัตโนมัติ**

| บรรทัดที่ ESP32 ส่งมา | การตีความ | ค่า `dataSource` ที่ได้ |
| :-- | :-- | :-- |
| `2840` | ช่อง A จากฮาร์ดแวร์ + ช่อง B จำลอง | `Live Hardware A (COM3) + Sim B` |
| `2840,1720` | ทั้งสองช่องจากฮาร์ดแวร์จริง (**Bonus**) | `Live Hardware A+B (COM3)` |
| ไม่มีพอร์ต / เปิดพอร์ตไม่ได้ | จำลองทั้งสองช่อง | `Simulation Mode (2 Channels)` |

โค้ดที่ทำหน้าที่แยกแยะอยู่ในคลาส `DualSerialBridgeWorker`:

```csharp
if (line.Contains(','))
{
    // รูปแบบ 2 ช่องจากฮาร์ดแวร์จริง : "2840,1720"
    var parts = line.Split(',');
    if (parts.Length >= 2 && int.TryParse(parts[0], out int vA) && int.TryParse(parts[1], out int vB))
        _stateStore.Update(vA, vB, $"Live Hardware A+B ({selectedPort})");
}
else if (int.TryParse(line, out int valueA))
{
    // รูปแบบช่องเดียว : ช่อง A จริง + ช่อง B (จำลอง หรือ ปรับเอง)
    var (valueB, labelB) = ResolveChannelB(1.2);
    _stateStore.Update(valueA, valueB, $"Live Hardware A ({selectedPort}) + {labelB}");
}
```

---

## 3. โครงสร้าง JSON API แบบ 2 ช่องสัญญาณ

**`GET /api/telemetry`** — ผลลัพธ์จริงที่บันทึกได้จากการรันเซิร์ฟเวอร์

```json
{
  "channelA": {
    "name": "Potentiometer (Hardware)",
    "gauge": "7-Segment",
    "rawValue": 1176,
    "voltage": 0.95,
    "percentage": 28.7
  },
  "channelB": {
    "name": "Secondary Sensor (Simulated)",
    "gauge": "Speedometer",
    "rawValue": 3518,
    "voltage": 2.84,
    "percentage": 85.9
  },
  "dataSource": "Simulation Mode (2 Channels)",
  "lastUpdated": "2026-09-08T04:30:09.5981541Z"
}
```

> สังเกตว่าค่า `channelA` (28.7%) และ `channelB` (85.9%) **ต่างกัน** — เป็นหลักฐานว่าทั้งสองช่อง
> แยกจากกันจริง ไม่ได้แสดงค่าเดียวกันซ้ำสองที่

**สูตรแปลงค่าที่ใช้ในทั้งสองช่อง** (ADC 12 บิต, ช่วง 0 – 4095)

$$\text{voltage} = \frac{\text{raw}}{4095} \times 3.3 \qquad\qquad \text{percentage} = \frac{\text{raw}}{4095} \times 100$$

**`GET /api/status`** — Endpoint เสริมสำหรับตรวจว่าเซิร์ฟเวอร์กำหนดคู่เกจ์ถูกต้อง

```json
{
  "gateway": "Kestrel Dual-Channel IoT Gateway",
  "studentId": "67030098",
  "leftGauge": "3 - Retro 7-Segment",
  "rightGauge": "1 - Analog Speedometer",
  "uptimeSeconds": 1197700,
  "serverTime": "2026-09-08T04:30:09.6834681Z"
}
```

---

## 4. หลักการทำงานของฟังก์ชัน JavaScript (ตามที่ Rubric กำหนดให้อธิบาย)

### 4.1 ลูปเดียวป้อนสองหน้าปัด

```javascript
async function pollTelemetry() {
    const res  = await fetch('/api/telemetry');
    const data = await res.json();

    updateLeftWidget(data.channelA.percentage);    // เกจ์ซ้าย  <- ช่อง A
    updateRightWidget(data.channelB.percentage);   // เกจ์ขวา   <- ช่อง B
    // ... อัปเดตตัวเลข RAW / Voltage / แถบสถานะ
}
setInterval(pollTelemetry, 150);
```

**ทำไมต้องยิง `fetch` ครั้งเดียวแล้วแจกข้อมูล ไม่ยิงแยกช่องละครั้ง**
เพราะการเรียกครั้งเดียวรับประกันว่าเกจ์ทั้งคู่แสดงข้อมูลจาก **snapshot เวลาเดียวกัน**
(ฝั่งเซิร์ฟเวอร์ก็ `lock` ครั้งเดียวแล้วอ่านทั้งสองช่องพร้อมกันเช่นกัน — Atomic Snapshot)
ถ้ายิงแยกสองครั้ง ค่า A กับ B อาจมาจากคนละช่วงเวลา ทำให้ข้อมูลไม่สอดคล้องกัน

คาบ 150 ms ≈ 7 ครั้งต่อวินาที เร็วพอให้รู้สึกเป็น Real-time แต่ไม่ถี่จนสร้างภาระเซิร์ฟเวอร์เกินจำเป็น

### 4.2 เกจ์ซ้าย — `updateLeftWidget()` (7-Segment)

ใช้หลักการ **Lookup Table (Truth Table)** จับคู่ตัวเลข 0–9 กับสถานะติด/ดับของหลอดทั้ง 7 เส้น
แทนการเขียน `if-else` ซ้อนกัน 10 ชั้น

```javascript
const SEGMENT_MAP = { 0:[1,1,1,1,1,1,0], 1:[0,1,1,0,0,0,0], /* ... */ 9:[1,1,1,1,0,1,1] };
const SEG_NAMES   = ['a','b','c','d','e','f','g'];
```

ขั้นตอนการทำงาน:
1. ปัดเศษเปอร์เซ็นต์เป็นจำนวนเต็ม แล้วจำกัดช่วง 0–99 ด้วย `Math.min` / `Math.max`
2. แยกหลักสิบด้วย `Math.floor(val / 10)` และหลักหน่วยด้วย `val % 10`
3. `setDigit()` วนตาม `SEG_NAMES` แล้วเพิ่ม/ลบคลาส `.active` ของแต่ละ `<polygon>`
4. CSS คลาส `.seg.active` เปลี่ยนสีเป็นนีออนพร้อม `drop-shadow` ให้เกิดแสงเรือง (Glow Effect)

> **ข้อจำกัดที่ทราบ:** หน้าปัดมี 2 หลักตามสเปกใบงาน (00–99%) เมื่อหมุนสุดถึง 100%
> หน้าจอจะค้างแสดง **99** — ไม่ใช่บั๊ก แต่เป็นข้อจำกัดจำนวนหลัก
> ตัวเลขทศนิยมเต็ม (100.0%) ยังอ่านได้จากบรรทัดใต้หน้าปัด

### 4.3 เกจ์ขวา — `updateRightWidget()` (Speedometer)

```javascript
function percentToAngle(pct) {
    return (pct / 100.0) * 180.0 - 90.0;
}
```

**การแปลงสเกลเชิงเส้น (Linear Mapping)** จากช่วง 0–100% ไปเป็นมุม −90° ถึง +90°
(ช่วงกวาดรวม 180° ตามรูปทรงครึ่งวงกลมของหน้าปัด)

| เปอร์เซ็นต์ | มุมที่ได้ | ทิศทางเข็ม |
| :--: | :--: | :-- |
| 0 % | −90° | ชี้ซ้ายสุด |
| 50 % | 0° | ชี้ตรงขึ้นบน |
| 100 % | +90° | ชี้ขวาสุด |

**ความนุ่มนวลมาจาก CSS ไม่ใช่ JavaScript** — JS เปลี่ยนค่ามุมเป็นก้าวกระโดดทุก 150 ms
ส่วน `transition: transform 0.12s` บนตัวเข็มเป็นตัวเติมภาพระหว่างก้าวให้กวาดลื่นไหล
ค่าเวลาของ transition ต้องใกล้เคียงกับคาบ polling
— ตั้งนานเกินไปเข็มจะตามมือไม่ทัน (หน่วง) ตั้งสั้นเกินไปจะเห็นการกระตุกเป็นขั้น

### 4.4 แผงควบคุมช่อง B — `sendControl()` และ `syncControlUI()`

ใช้เทคนิค **Query Parameter** ตามบทเรียนที่ 2.7 *"การรับคำสั่งควบคุม (Control Endpoints)"*

```javascript
async function sendControl(params) {
    await fetch('/api/control/channelb?' + new URLSearchParams(params));
}

sliderB.addEventListener('input', () =>
    sendControl({ mode: 'manual', value: sliderB.value }));
```

`URLSearchParams` ช่วยประกอบ query string ให้อัตโนมัติพร้อม encode อักขระพิเศษ
ปลอดภัยกว่าการต่อสตริงเอง

**ปัญหาที่ต้องแก้: ลูป polling แย่งหัวสไลเดอร์**
เนื่องจาก `pollTelemetry()` ยิงทุก 150 ms และเรียก `syncControlUI()` ซึ่งเขียนค่ากลับเข้าสไลเดอร์
ถ้าไม่ป้องกัน หัวสไลเดอร์จะถูกดึงกลับตลอดเวลาจนลากไม่ได้ จึงใช้ธง `isDragging` คั่นไว้

```javascript
let isDragging = false;
sliderB.addEventListener('pointerdown', () => { isDragging = true; });
sliderB.addEventListener('pointerup',   () => { isDragging = false; });

function syncControlUI(mode, manualValue) {
    // ... สลับคลาส active ของปุ่ม และเปิด/ปิดสไลเดอร์
    if (!isDragging && manualValue !== undefined) {
        sliderB.value = manualValue;   // อัปเดตเฉพาะตอนที่ผู้ใช้ไม่ได้กำลังลาก
    }
}
```

**ผลการทดสอบ Endpoint ควบคุม**

| คำสั่ง | ผลลัพธ์ |
| :-- | :-- |
| `?mode=manual&value=3500` | `{"mode":"manual","value":3500,"percentage":85.5}` ✅ |
| `?value=99999` | `{"mode":"manual","value":4095,...}` — Clamp ทำงาน ✅ |
| `?mode=xyz` | HTTP **400 Bad Request** พร้อมข้อความอธิบาย ✅ |
| `?mode=auto` | กลับไปใช้คลื่นไซน์ ✅ |

### 4.5 การจัดการเมื่อเซิร์ฟเวอร์หลุด

`try...catch` รอบ `fetch` จับข้อผิดพลาดแล้วเปลี่ยนแถบสถานะเป็น `⚠️ Server Disconnected` สีแดง
**แต่ไม่หยุดลูป** — เมื่อเซิร์ฟเวอร์กลับมา หน้าเว็บจะอัปเดตต่อเองโดยไม่ต้องกด refresh

---

## 5. การจัดวางแบบ Responsive (Dual-Pane Grid)

```css
.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 24px;
    max-width: 900px;
}
```

`auto-fit` ร่วมกับ `minmax(320px, 1fr)` ทำให้กริด **ยุบจาก 2 คอลัมน์เหลือ 1 คอลัมน์เองอัตโนมัติ**
เมื่อความกว้างไม่พอ โดยไม่ต้องเขียน `@media` query แยกเลยแม้แต่บรรทัดเดียว

**ผลการทดสอบจริงบนเบราว์เซอร์**

| ความกว้าง viewport | จำนวนคอลัมน์ | ผลลัพธ์ |
| :-- | :--: | :-- |
| 1200 px (เดสก์ท็อป) | 2 | การ์ดวางเคียงกันซ้าย-ขวา ✅ |
| 800 px (แท็บเล็ต) | 2 | ยังวางเคียงกันได้พอดี ✅ |
| 619 px (มือถือแนวนอน) | 1 | ยุบซ้อนกันอัตโนมัติ ✅ |

---

## 6. ผลการทดสอบความถูกต้อง

ทดสอบโดยล็อกค่าคงที่แล้วอ่านสถานะจริงของ DOM กลับมาตรวจ:

| ค่า % | Segment หลักสิบ | Segment หลักหน่วย | ตัวเลขที่อ่านได้ | มุมเข็ม | ตัวเลขกลางหน้าปัด |
| :--: | :-- | :-- | :--: | :--: | :-- |
| 0 | `abcdef` = 0 | `abcdef` = 0 | **00** | −90° | 0.0% |
| 25 | `abdeg` = 2 | `acdfg` = 5 | **25** | −45° | 25.0% |
| 50 | `acdfg` = 5 | `abcdef` = 0 | **50** | 0° | 50.0% |
| 75 | `abc` = 7 | `acdfg` = 5 | **75** | +45° | 75.0% |
| 100 | `abcdfg` = 9 | `abcdfg` = 9 | **99** ¹ | +90° | 100.0% |

¹ ข้อจำกัดของหน้าปัด 2 หลักตามสเปกใบงาน ไม่ใช่ข้อผิดพลาดของโปรแกรม

**ผลการ Build:** `Kestrel_Dual_Dashboard -> Build succeeded. 0 Warning(s), 0 Error(s)`

---

## 7. เกณฑ์การให้คะแนน (Rubric) และสิ่งที่ต้องส่ง

| เกณฑ์ | คะแนน | สถานะ |
| :-- | :---: | :-- |
| ความถูกต้องตามโจทย์เฉพาะบุคคล | 30 | ✅ เกจ์ซ้าย = 7-Segment, เกจ์ขวา = Speedometer ตรงตามการคำนวณ |
| การเชื่อมต่อและตอบสนองแบบเรียลไทม์ | 30 | ⏳ ต้องถ่ายคลิปตอนต่อ ESP32 จริง (ห้ามขึ้น Simulation Mode) |
| การจัดการข้อมูล 2 ช่องสัญญาณ | 20 | ✅ JSON `channelA` / `channelB` ครบถ้วน |
| ความสวยงามและ Responsive Design | 20 | ✅ Glow Effect + ยุบคอลัมน์อัตโนมัติ ทดสอบ 3 ขนาดจอแล้ว |

### สิ่งที่ต้องแนบก่อนส่งงาน

- [ ] **คลิปวิดีโอ 15–30 วินาที** ที่เห็นพร้อมกันทั้ง
      แถบรหัสนักศึกษา + ผลคำนวณคู่เกจ์บนหน้าเว็บ, นิ้วมือหมุน Potentiometer บนบอร์ด ESP32,
      เกจ์ซ้าย (7-Segment) เปลี่ยนตามมือ และเกจ์ขวา (Speedometer) ขยับตามช่อง B
- [ ] ภาพหน้าจอแดชบอร์ดขณะทำงานสมบูรณ์
- [ ] รายงานสรุปผลการทดลอง → [`Code/Lab8-5/REPORT.md`](../Code/Lab8-5/REPORT.md)
- [ ] ตรวจให้แถบสถานะขึ้น `📡 Live Hardware A (COM3) + Sim B` **ไม่ใช่** `Simulation Mode (2 Channels)`
      ถ้าขึ้น Simulation แปลว่าพอร์ต COM3 ถูกโปรแกรมอื่นยึดอยู่ (ESP-IDF Monitor
      หรือเซิร์ฟเวอร์ของใบงาน 8.3 / 8.4 ที่ยังเปิดค้าง) ต้องปิดให้หมดก่อน

### คะแนนพิเศษ (Bonus) — ต่อเซนเซอร์ตัวที่ 2 จริง

ฝั่งเซิร์ฟเวอร์รองรับไว้แล้ว **ไม่ต้องแก้โค้ด C# เลย** เพียงแก้เฟิร์มแวร์ให้ส่งค่าคู่คั่นด้วยจุลภาค:

1. ต่อเซนเซอร์ตัวที่สอง (LDR หรือ Potentiometer อีกตัว) เข้าขา **GPIO 35** (`ADC_CHANNEL_7`)
2. ใน `main.c` เพิ่มการ config ช่องที่สอง แล้วเปลี่ยนบรรทัด `printf` เป็น

```c
printf("%d,%d\n", (int)filtered_a, (int)filtered_b);
```

3. เซิร์ฟเวอร์จะตรวจพบเครื่องหมายจุลภาคแล้วสลับไปโหมด `Live Hardware A+B` เองอัตโนมัติ

---

## 📸 ภาพบันทึกผลการทดลอง

### คลิปสาธิตการทำงาน (GIF)

![แดชบอร์ดคู่ ช่อง A ตามมือหมุน Potentiometer และช่อง B ปรับด้วยสไลเดอร์](images/lab8-5-01-demo.gif)

**สิ่งที่เห็นในคลิป**

1. **แถบรหัสนักศึกษาและผลการคำนวณคู่เกจ์** อยู่ด้านบนสุดตลอดเวลา
   (`67030098 → N = 98` → เกจ์ซ้าย = 3 → 7-Segment, เกจ์ขวา = 1 → Speedometer)
   ตรงตามที่ Rubric ข้อ 1 กำหนดให้ถ่ายให้เห็น
2. **เกจ์ซ้าย (CH-A)** — ตัวเลข 7-Segment เปลี่ยนตามการหมุน Potentiometer B50K ที่ GPIO 32
3. **เกจ์ขวา (CH-B)** — เข็ม Speedometer ขยับตามสไลเดอร์ที่ปรับเอง (โหมด Manual)
   หรือแกว่งเป็นคลื่นไซน์เองในโหมด Auto
4. **ทั้งสองช่องขยับเป็นอิสระจากกัน** — เป็นหลักฐานว่าเป็น Dual-Channel จริง
   ไม่ได้แสดงค่าเดียวกันซ้ำสองที่
5. **แถบสถานะด้านล่าง** แสดง `dataSource` และเวลาที่อัปเดตล่าสุด

> รายงานผลการทดลองฉบับเต็ม → [`Code/Lab8-5/REPORT.md`](../Code/Lab8-5/REPORT.md)

---

&larr; [ใบงานที่ 8.4](ANSWER-Lab8-4.md) &nbsp;•&nbsp; [สารบัญคำตอบทั้งหมด](README.md)
