# รายงานผลการทดลอง ใบงานที่ 8-4
## แดชบอร์ดมาตรวัดความเร็ว SVG และสนามทดลองสร้างสรรค์

**รหัสนักศึกษา:** 67030098 &nbsp;|&nbsp; **ชื่อ:** Theeranat Phutiwanich
**คณะ:** School of Industrial Education and Technology, Technology Computer
**Branch:** `67030098-HW`

---

## 1. วัตถุประสงค์และสิ่งที่ทำได้

| วัตถุประสงค์ตามใบงาน | ผลการดำเนินการ |
| :-- | :-- |
| กำหนดค่า Kestrel ให้เสิร์ฟไฟล์หน้าเว็บ (Static Files) | ✅ เพิ่ม `app.UseFileServer();` ก่อน `app.Run();` |
| เขียนกราฟิกเวกเตอร์ด้วยแท็ก `<svg>` | ✅ Speedometer + 7-Segment เป็น SVG ล้วน 100% |
| เขียน JavaScript `fetch` แบบ Polling | ✅ `setInterval(pollTelemetry, 150)` |
| แปลงสเกลเชิงเส้นสู่มุมการหมุนด้วย CSS Transform | ✅ `percentToAngle()` + `transform: rotate()` |
| ใช้ความคิดสร้างสรรค์ปรับแต่งหน้าปัด | ✅ เพิ่ม 7-Segment (Creative Playground ตัวเลือก B) |

**หลักการ Zero External Dependency:** ทั้งหน้าเว็บไม่มีการโหลดไลบรารีภายนอกแม้แต่ตัวเดียว
ไม่มี `<script src="https://...">` ไม่มี CDN ไม่มี jQuery/Chart.js — กราฟิกทั้งหมดวาดด้วย
แท็ก SVG มาตรฐาน และแอนิเมชันใช้ CSS `transition` ล้วน จึงทำงานได้แม้ไม่มีอินเทอร์เน็ต
ซึ่งเป็นข้อกำหนดสำคัญของงาน Edge IoT Gateway

---

## 2. โครงสร้างหน้าจอ

หน้าแดชบอร์ดมี **2 หน้าปัด** วางเคียงกัน ทำงานจากข้อมูลชุดเดียวกัน

| ตำแหน่ง | หน้าปัด | ที่มา |
| :-- | :-- | :-- |
| ซ้าย | **Analog Speedometer** | กิจกรรมที่ 2 (หน้าปัดหลักของใบงาน) |
| ขวา | **Retro 7-Segment 2 หลัก** | สนามทดลองสร้างสรรค์ ตัวเลือก B |

**เหตุผลที่เลือกตัวเลือก B:** รหัส 67030098 ($N = 98$) เมื่อคำนวณตามกติกาของใบงาน 8-5
จะได้เกจ์ซ้าย = 3 (7-Segment) และเกจ์ขวา = 1 (Speedometer) ซึ่งตรงกับคู่นี้พอดี
ทำในใบงาน 8-4 ครั้งเดียวจึงนำไปต่อยอดใบงาน 8-5 ได้ทันทีโดยไม่ต้องเขียนวิดเจ็ตใหม่

เพิ่มเติมจากใบงาน: การ์ด **Alert Level** ที่เปลี่ยนสีตามระดับ (เขียว/เหลือง/แดง)
ดึงจากฟิลด์ `alertLevel` ที่ทำไว้เป็น Micro-Challenge ของใบงาน 8-3

---

## 3. ⚠️ ปัญหาสำคัญที่พบในโค้ดตัวอย่างของใบงาน

**อาการ:** ถ้าคัดลอกโค้ด `pollTelemetry()` จากสนามทดลองสร้างสรรค์มาวางทับตามใบงาน
**เข็มไมล์ Speedometer จะหยุดนิ่งทันที** เหลือเพียงหน้าปัดเดียวที่ขยับ

**สาเหตุ:** โค้ดตัวอย่างของ**ทั้งสามตัวเลือก (A, B และ C)** เขียนมาเป็นฟังก์ชันเต็ม
ที่ใช้แทนที่ของเดิม แต่ภายในไม่มีคำสั่งหมุนเข็ม

```javascript
// โค้ดตัวอย่างในใบงาน — ขาดบรรทัดหมุนเข็มไมล์
async function pollTelemetry() {
    const data = await response.json();
    document.getElementById('disp-raw').textContent = data.rawValue;
    update7Segment(data.percentage);        // มีแต่หน้าปัดใหม่
    // ไม่มี needle.style.transform = ... อีกต่อไป
}
```

**ผลกระทบ:** เสียคะแนนหัวข้อ *"ความสวยงามและความคิดสร้างสรรค์ของ SVG Dashboard"* (25%)
เพราะแดชบอร์ดเหลือหน้าปัดเดียวที่ทำงาน

**วิธีแก้ที่ใช้:** แยกการอัปเดตแต่ละหน้าปัดออกเป็นฟังก์ชันของตัวเอง
แล้วให้ `pollTelemetry()` เรียกทั้งหมดในรอบเดียว — เป็นการ**รวม** ไม่ใช่**เขียนทับ**

```javascript
async function pollTelemetry() {
    const res = await fetch('/api/telemetry');
    if (!res.ok) return;
    const data = await res.json();

    updateSpeedometer(data.percentage);   // หน้าปัดที่ 1 — ห้ามลืมบรรทัดนี้
    update7Segment(data.percentage);      // หน้าปัดที่ 2

    document.getElementById('disp-raw').textContent = data.rawValue;
    document.getElementById('disp-volt').textContent = data.voltage.toFixed(2) + ' V';
    updateAlert(data.alertLevel);
}
```

การยิง `fetch` **ครั้งเดียวต่อรอบ** แล้วแจกข้อมูลให้ทุกหน้าปัด ยังรับประกันว่าทั้งสองหน้าปัด
แสดงข้อมูลจากช่วงเวลาเดียวกันเสมอ ดีกว่าการยิงแยกหน้าปัดละครั้ง

---

## 4. หลักการทำงานของหน้าปัด

### 4.1 Speedometer — การแปลงสเกลเชิงเส้น (Linear Mapping)

```javascript
function percentToAngle(pct) {
    return (pct / 100.0) * 180.0 - 90.0;
}
```

| เปอร์เซ็นต์ | มุมที่ได้ | ทิศทางเข็ม |
| :--: | :--: | :-- |
| 0 % | −90° | ชี้ซ้ายสุด |
| 50 % | 0° | ชี้ตรงขึ้นบน |
| 100 % | +90° | ชี้ขวาสุด |

**ความนุ่มนวลมาจาก CSS ไม่ใช่ JavaScript** — JS เปลี่ยนค่ามุมเป็นก้าวกระโดดทุก 150 ms
ส่วน `transition: transform 0.15s` บนตัวเข็มเป็นตัวเติมภาพระหว่างก้าวให้กวาดลื่นไหล

```css
#needle {
    transform-origin: 150px 150px;   /* จุดหมุนต้องตรงกับแกนกลางหน้าปัดใน viewBox */
    transition: transform 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}
```

ค่าเวลา transition ควรใกล้เคียงกับคาบ polling
— ตั้งนานเกินไปเข็มจะตามมือไม่ทัน (หน่วง) ตั้งสั้นเกินไปจะเห็นการกระตุกเป็นขั้น

### 4.2 7-Segment — Lookup Table

ใช้ตารางความจริง (Truth Table) จับคู่ตัวเลข 0–9 กับสถานะติด/ดับของหลอดทั้ง 7 เส้น
แทนการเขียน `if-else` ซ้อนกัน 10 ชั้น

```javascript
const SEGMENT_MAP = { 0:[1,1,1,1,1,1,0], 1:[0,1,1,0,0,0,0], /* ... */ 9:[1,1,1,1,0,1,1] };
const SEG_NAMES   = ['a','b','c','d','e','f','g'];
```

แยกหลักสิบด้วย `Math.floor(val / 10)` และหลักหน่วยด้วย `val % 10`
แล้วเพิ่ม/ลบคลาส `.active` ของแต่ละ `<polygon>` ซึ่ง CSS จะเปลี่ยนเป็นสีนีออนพร้อม
`drop-shadow` ให้เกิดแสงเรือง (Glow Effect)

> **ข้อจำกัด:** หน้าปัดมี 2 หลักตามสเปกใบงาน (00–99%) เมื่อหมุนสุดถึง 100%
> หน้าจอจะค้างแสดง **99** — ไม่ใช่บั๊ก แต่เป็นข้อจำกัดจำนวนหลัก
> ตัวเลขทศนิยมเต็ม (100.0%) ยังอ่านได้จากตัวเลขกลางหน้าปัด Speedometer

### 4.3 การจัดการเมื่อเซิร์ฟเวอร์หลุด

`try...catch` รอบ `fetch` จับข้อผิดพลาดแล้วเปลี่ยนแถบสถานะเป็น `⚠️ Server Disconnected`
สีแดง **แต่ไม่หยุดลูป** — เมื่อเซิร์ฟเวอร์กลับมา หน้าเว็บอัปเดตต่อเองโดยไม่ต้อง refresh

---

## 5. ผลการทดสอบความถูกต้อง

ทดสอบโดยล็อกค่าคงที่แล้วอ่านสถานะจริงของ DOM กลับมาตรวจ

| ค่า % | มุมเข็ม (`transform`) | Segment หลักสิบ | Segment หลักหน่วย | ตัวเลขที่อ่านได้ |
| :--: | :--: | :-- | :-- | :--: |
| 0 | `rotate(-90deg)` | `abcdef` = 0 | `abcdef` = 0 | **00** |
| 25 | `rotate(-45deg)` | `abdeg` = 2 | `acdfg` = 5 | **25** |
| 50 | `rotate(0deg)` | `acdfg` = 5 | `abcdef` = 0 | **50** |
| 75 | `rotate(45deg)` | `abc` = 7 | `acdfg` = 5 | **75** |
| 100 | `rotate(90deg)` | `abcdfg` = 9 | `abcdfg` = 9 | **99** ¹ |

¹ ข้อจำกัดของหน้าปัด 2 หลักตามสเปกใบงาน ไม่ใช่ข้อผิดพลาดของโปรแกรม

**ผลการ Build**

```text
Kestrel_SVG_Dashboard -> Build succeeded. 0 Warning(s), 0 Error(s)
```

---

## 6. ปัญหาอื่นที่พบและวิธีแก้

### 6.1 ปีพุทธศักราชปนใน JSON

**อาการ:** ฟิลด์ `timestamp` ที่ส่งออกมาเป็น `"2569-09-08T04:20:33.501Z"` — ปี **2569**

**สาเหตุ:** `DateTime.ToString()` ใช้ `CurrentCulture` ของเครื่อง เมื่อเครื่องตั้งภาษาไทย (th-TH)
ปฏิทินเริ่มต้นจะเป็นพุทธศักราช ทำให้ค่าที่ได้ผิดรูปแบบมาตรฐาน **ISO 8601**
และฝั่ง JavaScript ใช้ `new Date()` แปลงไม่ได้

**วิธีแก้:** ระบุ `CultureInfo.InvariantCulture` เสมอเมื่อจัดรูปแบบวันเวลาที่จะส่งออกเป็น API

```csharp
timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
```

### 6.2 พอร์ต COM ถูกฮาร์ดโค้ดในโค้ด

ใบงานเขียน `string? targetPort = "COM24";` ไว้ในไฟล์ `.cs` โดยตรง ทำให้ต้องแก้โค้ด
และคอมไพล์ใหม่ทุกครั้งที่ย้ายเครื่อง จึงย้ายไปอ่านจาก `appsettings.json` แทน

```json
"SerialPort": { "Port": "COM3" }
```

เว้นว่าง `""` = ให้ระบบเลือกพอร์ตแรกที่ไม่ใช่ `COM1` ให้อัตโนมัติ

---

## 7. วิธีรัน

```bash
dotnet run --project Code\Lab8-4\Kestrel_SVG_Dashboard
```

เปิดเบราว์เซอร์ที่ `http://localhost:5062`

**ก่อนรัน** ต้องกด `Ctrl + ]` ออกจาก ESP-IDF Monitor และปิดเซิร์ฟเวอร์ของใบงานอื่นที่ยัง
เปิดค้างอยู่ให้หมดก่อน มิฉะนั้นพอร์ต COM จะถูกยึดและระบบจะตกไปอยู่ Simulation Mode

---

## 8. สิ่งที่ต้องแนบก่อนส่งงาน

- [x] คลิปสาธิตการทำงาน → [`Answer/images/lab8-4-01-demo.gif`](../../Answer/images/lab8-4-01-demo.gif)
- [ ] ภาพหน้าจอซอร์สโค้ด `Program.cs` และ `wwwroot/index.html`
- [x] รายงานผลการทดลอง (ไฟล์นี้)
- [ ] ตรวจให้แถบสถานะขึ้น `📡 Live Hardware (COM3)` **ไม่ใช่** `Simulation Mode (Sine Wave)`

> คำตอบของ Checkpoint และภาพประกอบทั้งหมด → [`Answer/ANSWER-Lab8-4.md`](../../Answer/ANSWER-Lab8-4.md)
