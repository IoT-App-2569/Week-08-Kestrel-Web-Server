# รายงานสรุปผลการทดลอง ใบงานปฏิบัติการที่ 08-5
## ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์ (Dual-Channel IoT Command Center)

**ชื่อ-นามสกุล:** [นูรีน ปิ่นคล้าย]  
**รหัสนักศึกษา:** 67030120  
**วิชา:** IoT Application Development  

---

### 1. การคำนวณเลือกชนิดเกจ์เฉพาะบุคคล (Student-ID Gauge Assignment)

จากรหัสนักศึกษา **67030120** พิจารณาเลข 3 ตัวท้าย คือ **$N = 120$** นำมาคำนวณตามสูตรที่กำหนด ดังนี้

* **การคำนวณเกจ์ฝั่งซ้าย (Left Gauge - Channel A):**
  $$\text{Left} = (N \bmod 4) + 1 = (120 \bmod 4) + 1 = 0 + 1 = \mathbf{1}$$
  * **ผลลัพธ์:** ได้เกจ์หมายเลข **1 - Analog Speedometer** (หน้าปัดเข็มไมล์กวาดมุม)

* **การคำนวณเกจ์ฝั่งขวา (Right Gauge - Channel B):**
  $$\text{Right} = (\lfloor N/4 \rfloor \bmod 4) + 1 = (\lfloor 120/4 \rfloor \bmod 4) + 1 = (30 \bmod 4) + 1 = 2 + 1 = \mathbf{3}$$
  * **ผลลัพธ์:** ได้เกจ์หมายเลข **3 - Retro 7-Segment Display** (หน้าจอดิจิทัลเรโทร 7 ส่วน)

> **สรุปชนิดของเกจ์ที่ใช้ในแดชบอร์ด:**
> - **เกจ์ซ้าย (Channel A):** Analog Speedometer (หน้าปัดเข็มไมล์)
> - **เกจ์ขวา (Channel B):** Retro 7-Segment Display (หน้าจอเรโทร 7 ส่วน)

---

### 2. ภาพหน้าจอแดชบอร์ดที่ทำงานสมบูรณ์ (Dashboard Screenshots)

<img width="895" height="650" alt="image" src="https://github.com/user-attachments/assets/c92734cc-f35d-4a27-8045-4bc24ba2331f" />

<img width="918" height="673" alt="image" src="https://github.com/user-attachments/assets/97b841d4-385f-4cac-a02c-af60c61b25eb" />

**รายละเอียดผลการทำงานบนแดชบอร์ด:**
1. **ฝั่งซ้าย (CH-A Speedometer):** แสดงการกวาดของเข็มไมล์เรืองแสงสีชมพู/แดงตามค่าเปอร์เซ็นต์แบบ Real-time พร้อมแสดงค่า Raw ADC และ Voltage
2. **ฝั่งขวา (CH-B 7-Segment):** แสดงตัวเลขดิจิทัลนีออนสีเขียวเรืองแสง 2 หลัก (00–99%) ซึ่งถอดรหัสจากค่าการจำลองสัญญาณคลื่นความถี่อย่างแม่นยำ
3. **การออกแบบเชิงตอบสนอง (Responsive Design):** หน้าจอใช้ CSS Grid จัดวางเลย์เอาต์คู่กันอย่างลงตัวแบบ Side-by-Side และรองรับการย่อขนาดหน้าจอแสดงผล

---

### 3. การอธิบายหลักการทำงานของฟังก์ชัน JavaScript ในการเชื่อมต่อข้อมูล

ในไฟล์ `wwwroot/index.html` มีการทำงานหลักของ JavaScript ในการเชื่อมต่อและประมวลผลข้อมูลแบ่งเป็น 3 ส่วนสำคัญ ดังนี้:

#### 3.1 การดึงข้อมูลดึงข้อมูลทางไกล (Real-Time Polling Engine)
ใช้ฟังก์ชัน `pollTelemetry()` ซึ่งถูกเรียกทำงานซ้ำทุกๆ 150 มิลลิวินาที ด้วย `setInterval(pollTelemetry, 150)` เพื่อดึงข้อมูล JSON ล่าสุดจาก Kestrel Web Server ผ่าน Endpoint `/api/telemetry`:

```javascript
async function pollTelemetry() {
    try {
        const res = await fetch('/api/telemetry');
        if (!res.ok) return;
        const data = await res.json();

        // 1. อัปเดตเกจ์ซ้าย (Analog Speedometer) ด้วยข้อมูล Channel A
        document.getElementById('val-left').textContent = data.channelA.percentage.toFixed(1) + '%';
        document.getElementById('sub-left').textContent = `Raw: ${data.channelA.rawValue} | ${data.channelA.voltage.toFixed(2)} V`;
        document.getElementById('needle').style.transform = `rotate(${percentToAngle(data.channelA.percentage)}deg)`;

        // 2. อัปเดตเกจ์ขวา (7-Segment Display) ด้วยข้อมูล Channel B
        document.getElementById('val-right').textContent = data.channelB.percentage.toFixed(1) + '%';
        document.getElementById('sub-right').textContent = `Raw: ${data.channelB.rawValue} | ${data.channelB.voltage.toFixed(2)} V`;
        update7Segment(data.channelB.percentage);

        // 3. อัปเดตสถานะแหล่งข้อมูล
        document.getElementById('disp-source').textContent = '📡 ' + data.dataSource;
    } catch (err) {
        console.error(err);
    }
}
