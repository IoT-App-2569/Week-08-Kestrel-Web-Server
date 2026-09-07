# รายงานผลการทดลอง — ใบงานที่ 08-5
## ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์ (Dual-Channel IoT Command Center)

**รหัสนักศึกษา:** 314 (ใช้ 3 ตัวท้าย)

---

## 1. วิธีคำนวณหาเกจ์ซ้าย-ขวา

ใช้เลขรหัสนักศึกษา 3 ตัวท้าย $N = 314$ แทนค่าตามสูตรที่กำหนด

| ตำแหน่งเกจ์ | สูตร | คำนวณ | ผลลัพธ์ |
|---|---|---|---|
| เกจ์ซ้าย (Left) | $(N \bmod 4) + 1$ | $314 \bmod 4 = 2 \rightarrow 2+1$ | **3** |
| เกจ์ขวา (Right) | $(\lfloor N/4 \rfloor \bmod 4) + 1$ | $\lfloor 314/4 \rfloor = 78 \rightarrow 78 \bmod 4 = 2 \rightarrow 2+1$ | **3** |

**ตรวจสอบกฎป้องกันเกจ์ซ้ำ (Anti-Collision Rule):**
คำนวณได้ Left = 3 และ Right = 3 ซึ่งเป็นเลขเดียวกัน → ตามกฎ **ให้บวกเกจ์ขวาเพิ่ม 1**
→ Right = 3 + 1 = **4**

### ผลลัพธ์สุดท้าย

| ตำแหน่ง | หมายเลขเกจ์ | ชนิดของเกจ์ |
|---|:---:|---|
| เกจ์ซ้าย (Channel A) | 3 | **Retro 7-Segment** |
| เกจ์ขวา (Channel B) | 4 | **Liquid Level Tank** |

---

## 2. สถาปัตยกรรมระบบ (System Architecture)

```
[ESP32 + Potentiometer] --(USB Serial 115200bps)--> [C# BackgroundService]
                                                            |
                                                    [DualChannelStateStore]
                                                    (Thread-safe, lock-based)
                                                            |
                                                    [/api/telemetry endpoint]
                                                            |
                                                    (JSON: channelA, channelB)
                                                            |
                                              [Browser: fetch() polling ทุก 150ms]
                                                            |
                                        [7-Segment (ซ้าย)]     [Liquid Tank (ขวา)]
```

- **Channel A** อ่านค่าจริงจาก Potentiometer บน ESP32 ผ่าน Serial Port
- **Channel B** เป็นสัญญาณจำลอง (Sine Wave) ที่สร้างขึ้นฝั่ง C# `DualSerialBridgeWorker`

---

## 3. หลักการทำงานของฟังก์ชัน JavaScript

### 3.1 การดึงข้อมูลแบบ Polling — `pollTelemetry()`

ฟังก์ชันนี้ถูกเรียกซ้ำทุก 150 มิลลิวินาทีด้วย `setInterval()` เพื่อดึงข้อมูลล่าสุดจาก endpoint `/api/telemetry` ด้วย `fetch()` แบบ asynchronous จากนั้นแยกค่า `data.channelA.percentage` และ `data.channelB.percentage` ส่งต่อให้ฟังก์ชันอัปเดตวิดเจ็ตแต่ละฝั่ง การ poll ถี่ทุก 150ms ทำให้การแสดงผลดูต่อเนื่องใกล้เคียง real-time โดยไม่ต้องใช้ WebSocket

### 3.2 เกจ์ซ้าย — `updateLeftWidget()` (Retro 7-Segment)

รับค่า `percentage` (0-100) แล้ว:
1. ปัดเศษและจำกัดค่าไว้ที่ 0-99 ด้วย `Math.min(99, Math.max(0, Math.round(percentage)))`
2. แยกหลักสิบและหลักหน่วยด้วยการหารและมอดูโล (`Math.floor(val/10)`, `val % 10`)
3. ส่งแต่ละหลักเข้าไปที่ `setDigit()` ซึ่งจะเปิดเปรียบเทียบกับ **Lookup Table** (`SEGMENT_MAP`) ที่เก็บรูปแบบเปิด/ปิดของเส้น 7 เส้น (a-g) สำหรับตัวเลข 0-9 แล้วสั่ง `classList.toggle('active', ...)` เพื่อเปิด/ปิดการเรืองแสงของแต่ละเส้น

### 3.3 เกจ์ขวา — `updateRightWidget()` (Liquid Level Tank)

รับค่า `percentage` แล้วคำนวณตำแหน่งระดับน้ำด้วยสมการ:

$$\text{fillHeight} = \left(\frac{\text{percentage}}{100}\right) \times \text{maxHeight}$$
$$\text{fillY} = \text{tankBottomY} - \text{fillHeight}$$

เนื่องจากระบบพิกัดของ SVG มีจุด (0,0) อยู่มุมซ้ายบน ดังนั้นเมื่อระดับน้ำสูงขึ้น ค่า `y` ของสี่เหลี่ยมน้ำ (`#water-fill`) ต้อง**ลดลง** จึงต้องคำนวณ `fillY` โดยการลบ ไม่ใช่การบวก จากนั้นใช้ `setAttribute('height', ...)` และ `setAttribute('y', ...)` ปรับขนาดสี่เหลี่ยมโดยตรง ซึ่งถูกครอบด้วย `<clipPath>` ทรงถังไว้แล้ว จึงไม่ล้นออกนอกกรอบ

### 3.4 การหมุนนุ่มนวล (Smooth Animation)

ทั้งสองวิดเจ็ตใช้ CSS `transition` (`transition: y 0.15s ease-out, height 0.15s ease-out;` และ `transition: opacity 0.08s ease` สำหรับ segment) แทนการกระโดดค่าแบบทันที ทำให้การเปลี่ยนแปลงแต่ละครั้งดูลื่นไหลแม้ข้อมูลจะอัปเดตทุก 150ms

---

## 4. ภาพหน้าจอแดชบอร์ด

> ![images](<Screenshot 2026-09-07 111214.png>)
> - ภาพขณะเกจ์ซ้าย (7-Segment) แสดงค่าจากการหมุน Potentiometer จริง
> - ภาพขณะเกจ์ขวา (Liquid Tank) แสดงระดับน้ำจากสัญญาณจำลอง

---

## 5. วิดีโอสาธิตการทำงาน

https://drive.google.com/file/d/1S_ygs4p8R8wJRFdtgQ0SS7i2jI0eqZIC/view?usp=sharing

---

## 6. สรุปผลการทดลอง

ระบบสามารถอ่านค่าจาก Potentiometer จริงผ่าน ESP32 และสตรีมผ่าน USB Serial มายัง C# Kestrel Server ได้อย่างเสถียร โดยข้อมูลถูกจัดเก็บใน `DualChannelStateStore` แบบ thread-safe เพื่อรองรับการอ่าน-เขียนพร้อมกันจาก background worker และ web request จากนั้นส่งออกเป็น JSON ผ่าน endpoint เดียว (`/api/telemetry`) ที่รวมทั้งสองช่องสัญญาณไว้ด้วยกัน ฝั่งเว็บใช้เทคนิค Polling ทุก 150ms ในการดึงข้อมูลมาอัปเดตวิดเจ็ตทั้งสองฝั่งแบบเกือบเรียลไทม์ ตรงตามเกจ์ที่คำนวณได้จากรหัสนักศึกษา 314 คือ **7-Segment (ซ้าย)** และ **Liquid Tank (ขวา)**
