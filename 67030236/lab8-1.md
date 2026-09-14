#### 📋 [Checkpoint 3.1: ทดสอบกลไก Fallback Simulation]
1. ขณะที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์
2. กด Refresh (F5) บนหน้าเบราว์เซอร์หลายๆ ครั้งติดต่อกัน สังเกตว่า:
   - ค่า `dataSource` เปลี่ยนเป็นอะไร?
   - ค่า `rawValue` ยังเปลี่ยนได้อยู่หรือไม่ และเปลี่ยนในลักษณะใด?
   - **คำตอบ:** ยังคง เปลี่ยนค่าได้อย่างต่อเนื่อง โดยค่าจะแกว่งขึ้น-ลงเป็นลูกคลื่นตามฟังก์ชันทางคณิตศาสตร์แบบคลื่นไซน์ (Sine Wave) ในช่วงค่าประมาณ 0 ถึง 4095 แบบวนรอบเรียบลื่นทุกๆ 100 millisecond

---

## 🎯 ภารกิจท้าทาย (Micro-Challenge)
- ให้นักศึกษาเพิ่มฟิลด์ `alertLevel` เข้าไปในผลลัพธ์ JSON ของ `/api/telemetry`:
  - ถ้า `percentage` มากกว่า 85.0% ให้ส่งค่า `"DANGER (HIGH)"`
  - ถ้า `percentage` ระหว่าง 70.0% - 85.0% ให้ส่งค่า `"WARNING"`
  - ถ้า `percentage` ต่ำกว่า 70.0% ให้ส่งค่า `"NORMAL"`
- บันทึกภาพหน้าจอเบราว์เซอร์ขณะหมุนไปที่ระดับต่างๆ เพื่อแสดงว่าฟิลด์ `alertLevel` ทำงานถูกต้อง

  ภาพแสดงค่า DANGER
  <img width="738" height="518" alt="image" src="https://github.com/user-attachments/assets/c7a5ef45-8b4b-44a5-80f6-e8018e25b868" />

  ภาพแสดงค่า WARNING
  <img width="775" height="491" alt="image" src="https://github.com/user-attachments/assets/0e6a5e58-c6b8-455b-8e8f-da65f5dcf323" />

  ภาพแสดงค่า NARMAL
  <img width="500" height="435" alt="image" src="https://github.com/user-attachments/assets/57aa211b-d7df-4920-b5f8-f84d06e46be6" />

