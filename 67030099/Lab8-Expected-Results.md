# Week 08: ผลที่คาดว่าจะได้ (Lab 8.1 - 8.5)

> **สถานะ:** ส่วนที่เขียนว่า "ทดสอบบนคอมพิวเตอร์แล้ว" รันจริงโดยไม่มี ESP32 ส่วนอื่นเป็นผลที่คาดว่าจะได้ ยังไม่ได้ทดสอบกับบอร์ดจริง ไม่มีผลใดในไฟล์นี้แทนผลจากบอร์ดหรือภาพหน้าจอของนักศึกษาได้

## ก่อนรัน

- **เลือกพอร์ตให้ถูก:** เครื่องนี้มี Bluetooth COM3, COM4, COM6, COM7 ถ้าปล่อยให้โปรแกรมเลือกเอง มันอาจเปิดพอร์ต Bluetooth แล้วค้างที่ `Initializing` จึงต้องใส่พอร์ตของ ESP32 ตามตาราง
- **ปิด Serial Monitor ก่อนรัน Kestrel** มิฉะนั้นจะเปิดพอร์ตไม่ได้

| Lab | ตำแหน่งที่แก้พอร์ต |
| :--- | :--- |
| 8.3 | `targetPort` ใน `Program.cs` |
| 8.4 | `explicitPort` ใน `Program.cs` |
| 8.5 | `explicitPort` ใน `Program.cs` |

## Lab 8.1: Kestrel Minimal API (ทดสอบบนคอมพิวเตอร์แล้ว)

| Endpoint | ผลที่ได้จากการรันจริง |
| :--- | :--- |
| `/` | `Welcome to IoT Edge Gateway!` |
| `/api/status` | `{"gateway":"ESP32-EdgeGateway","status":"Online",...,"isHealthy":true}` |
| `/api/led/on` | `{"device":"LED_D2","requestedState":"on","actionResult":"TURN ON",...}` |
| `/api/student` | `{"studentId":"67030116","studentName":"REPLACE_WITH_ENGLISH_NAME","faculty":"REPLACE_WITH_FACULTY_AND_PROGRAM",...}` |

**ต้องแก้ก่อนส่ง:** `studentName` และ `faculty` ใน `Lab8-1/Kestrel_API/Program.cs`

## Lab 8.2: ESP32 ADC Stream (คาดการณ์)

- Serial แสดงตัวเลขบรรทัดละค่า ประมาณ 20 ค่าต่อวินาที ที่ 115200 bps
- หมุน Potentiometer สุดด้านหนึ่งได้ค่าใกล้ 0 สุดอีกด้านได้ค่าใกล้ 4095 กึ่งกลางประมาณ 1800 - 2300
- EMA ทำให้ค่านิ่งขึ้น แต่ตามการหมุนช้าลงเล็กน้อย
- Micro-Challenge LDR: ค่าเปลี่ยนตามแสง ทิศทางขึ้นกับการต่อวงจรแบ่งแรงดัน

## Lab 8.3: Serial Bridge

**ทดสอบบนคอมพิวเตอร์แล้ว (โหมดจำลอง โดยชี้ไปยังพอร์ตที่ไม่มีอยู่):**

```json
{"sensor":"ESP32-Potentiometer","rawValue":2246,"voltage":1.81,"percentage":54.8,"alertLevel":"NORMAL","dataSource":"Simulation Mode (Sine Wave)","timestamp":"2026-11-05T10:06:08.402Z"}
```

ค่า `alertLevel` ตามเงื่อนไข Micro-Challenge (ค่าดิบในตารางคำนวณจากสูตร ยังไม่ได้วัดจากบอร์ด):

| rawValue | percentage | alertLevel |
| :---: | :---: | :---: |
| 2048 | 50.0 | `NORMAL` |
| 3000 | 73.3 | `WARNING` |
| 3600 | 87.9 | `DANGER (HIGH)` |

**คาดการณ์เมื่อมีบอร์ด:** `dataSource` เป็น `Live Hardware (COMx)` และค่าตามการหมุน

**Checkpoint 3.1 (คาดการณ์):** เมื่อถอดสาย `dataSource` ควรเปลี่ยนเป็น `Simulation Mode (Sine Wave)` และ `rawValue` ขึ้นลงแบบคลื่น Sine เคสนี้ต้องตั้ง `targetPort` แล้วเท่านั้น

## Lab 8.4: SVG Dashboard

**ทดสอบบนคอมพิวเตอร์แล้ว:** Build ผ่านและ `/api/status`, `/api/telemetry` ตอบ HTTP 200

**คาดการณ์เมื่อมีบอร์ด:** เข็ม Speedometer และแถบ VU กวาดตามการหมุน `alertLevel` ของ Lab นี้ใช้ `CRITICAL_HIGH` ที่ 85% ขึ้นไป ตามโค้ดตัวอย่าง

## Lab 8.5: Dual-Channel Command Center

การคำนวณเกจ์จากรหัส 67030116 (N = 116):

- Left = (116 mod 4) + 1 = 0 + 1 = **1 (Analog Speedometer)**
- Right = (⌊116 / 4⌋ mod 4) + 1 = (29 mod 4) + 1 = 1 + 1 = **2 (Audio VU Meter)**
- ค่าไม่ซ้ำกัน จึงไม่ต้องใช้กฎ Anti-Collision

**ทดสอบบนคอมพิวเตอร์แล้ว (โหมดจำลอง):** หน้าเว็บแสดง Speedometer ด้านซ้าย, VU Meter 10 ดวงด้านขวา และรหัสนักศึกษาพร้อมวิธีคำนวณ

```json
{"channelA":{"name":"Simulated Sensor A","rawValue":168,"voltage":0.14,"percentage":4.1},"channelB":{"name":"Simulated Sensor B","rawValue":2962,"voltage":2.39,"percentage":72.3},"dataSource":"Simulation Mode (2 Channels)"}
```

**คาดการณ์เมื่อมีบอร์ด:** ช่อง A ตามการหมุน Potentiometer ช่อง B เป็นสัญญาณจำลอง ใช้เฟิร์มแวร์จาก `Lab8-4/ESP32_ADC_Stream` ได้เลย ต้องส่งคลิปวิดีโอ 15 - 30 วินาทีจากการรันจริง

## ที่มาของโค้ด

โค้ด Lab 8.2 - 8.5 มาจาก `Example_codes` ที่อาจารย์ลบออกในคอมมิต `6ad1619` จึงกู้จาก Git History และจัดโฟลเดอร์ตาม Readme ข้อ 8 สิ่งที่แก้เพิ่ม:

- **Lab 8.1:** ใส่รหัสนักศึกษา และเพิ่ม `RollForward` ให้รันบน .NET 10 ได้
- **Lab 8.3:** เพิ่ม `alertLevel`, เปลี่ยน `targetPort` เป็น `null` และแก้ปี พ.ศ. ใน `timestamp` (เดิมแสดง 2569)
- **Lab 8.4:** เพิ่ม `RollForward`
- **Lab 8.5:** เปลี่ยนเกจ์ขวาเป็น VU Meter, เพิ่มรหัสบนหน้าเว็บ, เพิ่ม `explicitPort` และ `RollForward`
