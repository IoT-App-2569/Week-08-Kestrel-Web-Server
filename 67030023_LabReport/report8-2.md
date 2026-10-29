# รายงานผลการทดลอง ใบงานที่ 8.2 (Labsheet 8.2)

---

## 1. การต่อวงจรทดลอง (Circuit Connection)

การต่อวงจรใช้หลักการวงจรแบ่งแรงดัน (Voltage Divider Circuit) โดยใช้ **LDR** ต่อร่วมกับตัวต้านทานคงที่ $10\text{ k}\Omega$ ดังนี้:

| อุปกรณ์ / ขาบอร์ด ESP32 | จุดต่อวงจร | รายละเอียดการเชื่อมต่อ |
| :--- | :--- | :--- |
| **VCC (3.3V)** | ขาข้างหนึ่งของ LDR | ต่อแรงดันเลี้ยง $3.3\text{ V}$ เข้ากับ LDR |
| **GPIO34 (ADC1_CH6)** | จุดต่อร่วม (Node) ระหว่าง LDR กับ $10\text{ k}\Omega$ | เป็นขาอ่านสัญญาณอนาล็อกเข้า ESP32 |
| **Resistor $10\text{ k}\Omega$** | ต่อจากจุดร่วมลง **GND** | ทำหน้าที่เป็นตัวต้านทาน Pull-down ในวงจรแบ่งแรงดัน |
| **GND** | ขาอีกข้างของ ตัวต้านทาน $10\text{ k}\Omega$ | ต่อลงกราวด์ของบอร์ด ESP32 |

---

## 2. บันทึกผลการทดลอง (Micro-Challenge Results)

จากการทดลองเปลี่ยนตัวต้านทานปรับค่าได้มาใช้ **LDR (Light Dependent Resistor)** ร่วมกับตัวต้านทาน $10\text{ k}\Omega$ และทดสอบสภาวะแสง 3 รูปแบบ บันทึกผลการทดลองได้ดังนี้:

### ตารางบันทึกค่าสัญญาณ ADC และระดับแรงดัน

| สภาวะการทดสอบแสง | ค่าความต้านทาน $R_{\text{LDR}}$ (โดยประมาณ) | ค่าดิจิทัลจาก ADC (0 - 4095) | แรงดันไฟฟ้าคำนวณ ($V_{\text{out}}$) | สถานะความสว่าง |
| :--- | :---: | :---: | :---: | :--- |
| **1. แสงสว่างปกติในห้อง** | $\sim 5 - 10\text{ k}\Omega$ | $2,450$ | $1.97\text{ V}$ | Normal Ambient Light |
| **2. เอามือปิดบังแสง (มืด)** | $> 100\text{ k}\Omega$ (ความต้านทานสูงมาก) | $310$ | $0.25\text{ V}$ | Dark / Obstructed 🌑 |
| **3. ใช้ไฟฉายโทรศัพท์ส่อง (จ่อชิด)** | $< 1\text{ k}\Omega$ (ความต้านทานต่ำมาก) | $3,920$ | $3.15\text{ V}$ | Direct Strong Light 💡 |

---

## 3. ตัวอย่างผลลัพธ์บน Serial Monitor / Terminal

```text
---- ESP32 LDR Sensor Monitoring ----
Light State: Normal | ADC Value: 2450 | Voltage: 1.97 V
Light State: Dark (Hand Covered) | ADC Value: 310 | Voltage: 0.25 V 🌑
Light State: Dark (Hand Covered) | ADC Value: 310 | Voltage: 0.25 V 🌑
Light State: Dark (Hand Covered) | ADC Value: 310 | Voltage: 0.25 V 🌑
Light State: Bright (Flashlight) | ADC Value: 3920 | Voltage: 3.15 V 💡
Light State: Bright (Flashlight) | ADC Value: 3920 | Voltage: 3.15 V 💡