# สารบัญคำตอบใบงานสัปดาห์ที่ 8

**รหัสนักศึกษา:** 67030098 &nbsp;|&nbsp; **ชื่อ:** Theeranat Phutiwanich &nbsp;|&nbsp; **Branch:** `67030098-HW`

แยกคำตอบเป็นไฟล์ละใบงาน ครอบคลุมทุกช่องที่ต้องกรอก — Checkpoint, บันทึกผลการทดลอง,
ภารกิจท้าทาย (Micro-Challenge) และคำถามท้ายการทดลอง (Review Questions)

---

## รายการไฟล์

| ใบงาน | ไฟล์คำตอบ | หัวข้อที่ตอบ |
| :-- | :-- | :-- |
| **8.1** | [ANSWER-Lab8-1.md](ANSWER-Lab8-1.md) | Checkpoint 1.1 · บันทึกผล Terminal · Micro-Challenge · **Review Questions 3 ข้อ** |
| **8.2** | [ANSWER-Lab8-2.md](ANSWER-Lab8-2.md) | วงจรที่ต่อจริง (GPIO 32 / B50K) · **Checkpoint 2.1 (2 ข้อ)** · Micro-Challenge LDR |
| **8.3** | [ANSWER-Lab8-3.md](ANSWER-Lab8-3.md) | **Checkpoint 3.1 (3 ข้อ)** · Micro-Challenge `alertLevel` |
| **8.4** | [ANSWER-Lab8-4.md](ANSWER-Lab8-4.md) | Creative Playground · สูตร Linear Mapping · กับดัก `pollTelemetry` |
| **8.5** | [ANSWER-Lab8-5.md](ANSWER-Lab8-5.md) | **การคำนวณคู่เกจ์จากรหัส** · Dual-Channel JSON · หลักการ JavaScript · Rubric |

ทุกไฟล์มีหัวข้อ **📸 ภาพบันทึกผลการทดลอง** อยู่ท้ายไฟล์ ใบงานละ 6 ช่อง
ให้บันทึกภาพลงโฟลเดอร์ [`images/`](images/) ตามชื่อไฟล์ที่ระบุไว้ใต้แต่ละช่อง แล้วภาพจะขึ้นเอง

---

## สรุปคำถามทั้งหมดและคำตอบแบบสั้น

| ใบงาน | คำถาม | คำตอบโดยสรุป |
| :--: | :-- | :-- |
| 8.1 | Checkpoint 1.1 — กด `Ctrl+C` แล้ว Refresh เกิดอะไรขึ้น | `ERR_CONNECTION_REFUSED` เพราะ Kestrel ฝังอยู่ในโปรเซสเดียวกับแอป โปรเซสตาย = พอร์ตถูกคืนทันที |
| 8.1 | Review Q1 — `builder` กับ `app` ต่างกันอย่างไร | `builder` = ช่วงตั้งค่าก่อน `Build()` (คอนฟิก/DI) · `app` = ช่วงทำงานจริง (route + middleware + `Run()`) |
| 8.1 | Review Q2 — Minimal API vs LAMP สำหรับ IoT Gateway | Minimal API ชนะ เพราะ PHP เป็น Stateless per-request เปิดพอร์ต Serial ค้างไว้ไม่ได้ |
| 8.1 | Review Q3 — ประโยชน์ของ `/api/` prefix | กัน route ชนไฟล์ใน `wwwroot` (สำคัญมากตอนใบงาน 8.4) · ทำ versioning · ตั้ง CORS/Auth เป็นกลุ่ม |
| 8.2 | Checkpoint 2.1 ข้อ 1 — `adc_oneshot_read()` คืนค่าแบบใด | คืน `esp_err_t` ส่วนค่าจริงเป็น `int` ผ่าน out param — ช่วง **0 – 4095** ที่ `ADC_BITWIDTH_12` |
| 8.2 | Checkpoint 2.1 ข้อ 2 — ทำไมต้อง `Ctrl + ]` ก่อนรัน C# | พอร์ต COM ใช้ได้ทีละโปรเซส ถ้า monitor ยึดอยู่ C# จะได้ `UnauthorizedAccessException` แล้วตกไป Simulation Mode |
| 8.3 | Checkpoint 3.1 — ถอดสาย USB แล้ว `dataSource` เปลี่ยนเป็นอะไร | `Simulation Mode (Sine Wave)` |
| 8.3 | Checkpoint 3.1 — `rawValue` ยังเปลี่ยนไหม | ยังเปลี่ยน แต่แกว่งเป็นคลื่นไซน์เอง คาบ $2\pi/1.5 \approx 4.19$ วินาที ไม่ตอบสนองต่อมือหมุน |
| 8.4 | Creative Playground เลือกอะไร | **ตัวเลือก B — Retro 7-Segment** (ตรงกับเกจ์ซ้ายที่ใบงาน 8.5 กำหนดให้พอดี) |
| 8.5 | คู่เกจ์ที่คำนวณได้จากรหัส 67030098 | $N = 98$ → ซ้าย $= (98 \bmod 4)+1 = \mathbf{3}$ (7-Segment) · ขวา $= (\lfloor 98/4 \rfloor \bmod 4)+1 = \mathbf{1}$ (Speedometer) |

---

## ตารางสรุปสิ่งที่ต้องส่งทั้ง 5 ใบงาน

| ใบงาน | Checkpoint | Micro-Challenge | หลักฐานที่ต้องแนบ |
| :--: | :-- | :-- | :-- |
| **8.1** | 1.1 + Review Q 3 ข้อ | `/api/student` | ภาพหน้าจอ JSON + ภาพโค้ด + บันทึก terminal ของ `/api/led/{state}` |
| **8.2** | 2.1 (2 ข้อ) | เปลี่ยนเป็น LDR | ภาพถ่ายวงจร + ภาพหน้าจอ Monitor (ส่องไฟ / ปิดมือ) |
| **8.3** | 3.1 (ถอดสาย USB) | ฟิลด์ `alertLevel` | ภาพหน้าจอ 3 ระดับ (NORMAL / WARNING / DANGER) |
| **8.4** | — | Creative Widget 1 อย่าง | **วิดีโอ 15–30 วินาที** + ภาพโค้ด + รายงาน |
| **8.5** | — | (Bonus) เซนเซอร์ตัวที่ 2 | **วิดีโอ 15–30 วินาที** ที่เห็นรหัสนักศึกษา + คู่เกจ์ + [REPORT.md](../Code/Lab8-5/REPORT.md) |

**อย่าลืม:** commit แยกทีละใบงานตามกฎ Project Isolation / Anti-Cheating
ในหัวข้อที่ 8 ของ [Readme.md](../Readme.md)

---

## ลิงก์ที่เกี่ยวข้อง

- โค้ดทั้งหมด → [`Code/`](../Code) &nbsp;•&nbsp; [`Code/README.md`](../Code/README.md)
- รายงานใบงาน 8.5 → [`Code/Lab8-5/REPORT.md`](../Code/Lab8-5/REPORT.md)
