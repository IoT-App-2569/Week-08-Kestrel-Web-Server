## ส่งงานและประเมินผล (Submission & Grading Rubric)

### สิ่งที่ต้องส่ง
1. **คลิปวิดีโอสาธิตการทำงาน (15 - 30 วินาที)**
   * ถ่ายให้เห็น **รหัสนักศึกษา** และผลการคำนวณคู่เกจ์
   * นิ้วมือหมุน Potentiometer บนบอร์ด ESP32 จริง แล้วเกจ์ฝั่งซ้ายกวาดตามมืออย่างชัดเจน
   * เกจ์ฝั่งขวาขยับตามข้อมูลช่อง B (Simulation หรือ เซนเซอร์ตัวที่ 2)

     คลิปวิดีโอ [https://youtube.com/shorts/g0rNNZSKnW0?feature=share](https://youtube.com/shorts/g0rNNZSKnW0?feature=share)
2. **รายงาน (markdown/pull request) สรุปผลการทดลอง**
   * ระบุเลขรหัสนักศึกษาและแสดงวิธีคำนวณหาเกจ์ซ้าย-ขวา
     <img width="620" height="260" alt="image" src="https://github.com/user-attachments/assets/27bf7683-02eb-453d-aa90-4d92733f71aa" />

   * ภาพหน้าจอแดชบอร์ดที่ทำงานสมบูรณ์
     <img width="1917" height="932" alt="image" src="https://github.com/user-attachments/assets/eb865df6-f148-4d79-9747-ecdb13f1e780" />

   * อธิบายหลักการทำงานของฟังก์ชัน JavaScript ในการเชื่อมต่อข้อมูล
  ```c
การดึงข้อมูลแบบ Real-time (Asynchronous Polling): ใช้ฟังก์ชัน pollTelemetry() ร่วมกับ setInterval() เพื่อส่งคำสั่ง fetch('/api/telemetry') ไปดึงข้อมูล telemetry จาก ESP32 ทุกๆ 150 มิลลิวินาที โดยไม่ทำให้หน้าเว็บค้าง

การแปลงข้อมูล (JSON Parsing): แปลงข้อมูลตอบกลับจากเซิร์ฟเวอร์ให้อยู่ในรูปแบบ JSON Object แล้วส่งต่อค่าเปอร์เซ็นต์ของ channelA และ channelB ไปยังฟังก์ชันแสดงผล

การอัปเดตหน้าจอและกราฟิก (DOM & SVG Update): ฟังก์ชัน updateLeftWidget และ updateRightWidget นำค่าตัวเลขไปอัปเดตข้อความบนหน้าจอ พร้อมคำนวณแปลงค่าเปอร์เซ็นต์ (0–100%) เพื่อปรับพิกัดกราฟิก SVG (เช่น ความสูงของระดับน้ำ หรือมุมหมุนของเข็ม) ให้แสดงผลเปลี่ยนแปลงแบบเคลื่อนไหวทันที
```
