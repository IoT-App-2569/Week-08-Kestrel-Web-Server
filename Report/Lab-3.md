# รายงานผลการทดลองที่ 8.3: สะพานเชื่อมฮาร์ดแวร์จริงสู่เว็บเซิร์ฟเวอร์ (Hardware Serial Bridge)

**ชื่อ-นามสกุล:** นาย พรพรม สุขใจมิตร
**รหัสนักศึกษา:** 67030323

---

## [Checkpoint 3.1: ทดสอบกลไก Fallback Simulation]

**คำถาม:** ขณะที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์ กด Refresh (F5) บนหน้าเบราว์เซอร์หลายๆ ครั้งติดต่อกัน สังเกตว่า:

* ค่า `dataSource` เปลี่ยนเป็นอะไร?
* ค่า `rawValue` ยังเปลี่ยนได้อยู่หรือไม่ และเปลี่ยนในลักษณะใด?

**คำตอบ:** ค่า `dataSource` จะเปลี่ยนจาก "Live Hardware " เป็น **"Simulation Mode"** ส่วนค่า `rawValue` ยังคงเปลี่ยนแปลงอยู่ โดยจะสวิงขึ้นลงอย่างเป็นจังหวะตามฟังก์ชันคลื่นไซน์ ที่รันอยู่ใน Background Worker เพื่อจำลองสัญญาณหลอกให้ระบบทำงานต่อไปได้โดยไม่ล่ม

---

## ภารกิจท้าทาย (Micro-Challenge)

*เพิ่มฟิลด์ `alertLevel` เข้าไปในผลลัพธ์ JSON ของ `/api/telemetry` ตามระดับ percentage*

**โค้ดส่วนที่แก้ไขใน Endpoint `/api/telemetry` (Program.cs):**

```csharp
    string alert = "NORMAL";
    if (percent > 85.0) {
        alert = "DANGER (HIGH)";
    } else if (percent >= 70.0) {
        alert = "WARNING";
    }
```

**ภาพหน้าจอผลลัพธ์บนเบราว์เซอร์:**

1. สถานะ **DANGER (HIGH)** (เมื่อ Percentage > 85.0%)

   `[แทรกรูปภาพที่นี่]`

2. สถานะ **WARNING** (เมื่อ Percentage อยู่ระหว่าง 70.0% - 85.0%)

   `[แทรกรูปภาพที่นี่]`

3. สถานะ **NORMAL** (เมื่อ Percentage < 70.0%)

  ![alt text](/Report/img/image-8.png)
