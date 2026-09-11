# สรุปผลการทดลอง ใบงานที่ 8.3 (Labsheet 8.3)
## สะพานเชื่อมฮาร์ดแวร์จริงสู่เว็บเซิร์ฟเวอร์ (Hardware Serial Bridge)

### 🌟 กิจกรรมที่ 5: ทดสอบการทำงานสดๆ (The Magic Moment)

1. เสียบสาย USB ESP32 เข้ากับคอมพิวเตอร์
2. พิมพ์คำสั่งสั่งรัน:
   ```bash
   dotnet run
   ```
   *สังเกต Terminal: ระบบจะตรวจพบพอร์ต COM และขึ้นข้อความ `✅ เชื่อมต่อฮาร์ดแวร์สำเร็จ`*
3. เปิดเบราว์เซอร์ไปที่: `http://localhost:5000/api/telemetry`
4. **ทดสอบหมุนตัวต้านทานปรับค่าได้บนโต๊ะ แล้วกด Refresh (F5) บนเบราว์เซอร์:**
   - **สิ่งที่สังเกตได้:** ตัวเลข `rawValue`, `voltage`, และ `percentage` บนหน้าจอเบราว์เซอร์จะเปลี่ยนไปตามมุมที่มือนักศึกษาหมุนบอร์ดเป๊ะๆ!

> 💡 **จุดว้าวที่ 3:** โลกกายภาพ (นิ้วมือหมุน Volume) ได้ส่งสัญญาณไฟฟ้า ทะลุสาย USB ผ่าน .NET Web Server และมาปรากฏเป็นข้อมูลบนหน้าเว็บได้อย่างสมบูรณ์แบบแล้ว!

- หมุนไปทางซ้ายสุด

<img width="1600" height="860" alt="Picture (6)" src="https://github.com/user-attachments/assets/cb8814cf-6f8e-4f35-bc4f-f9c374086ecf" />


- หมุนไปทางขวาสุด

<img width="1600" height="860" alt="Picture (7)" src="https://github.com/user-attachments/assets/e1da2f71-6130-44ea-ae88-33c3c848f930" />

---

### Checkpoint 3.1 ทดสอบกลไก Fallback Simulation
1. ที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์ 
2. Refresh (F5) บนหน้าเบราว์เซอร์หลายๆ ครั้งติดต่อกัน สังเกตว่า
- ค่า dataSource เปลี่ยนเป็นอะไร?
> `dataSource` จะเปลี่ยนเป็น Simulation Mode (โหมดจำลอง)
- ค่า rawValue ยังเปลี่ยนได้อยู่หรือไม่ เปลี่ยนในลักษณะใด?
> `rawValue` จะยังคงเปลี่ยนไปเรื่อยๆ เหมือนเดิม

---

## 🎯 ภารกิจท้าทาย (Micro-Challenge)

- `percentage` ต่ำกว่า 70.0% ให้ส่งค่า `"NORMAL"`

<img width="1600" height="860" alt="Picture (8)" src="https://github.com/user-attachments/assets/f8316458-d6e7-4dfd-8cb7-d4a579a473a7" />

---
- `percentage` ระหว่าง 70.0% - 85.0% ให้ส่งค่า `"WARNING"`

<img width="1600" height="860" alt="Picture (9)" src="https://github.com/user-attachments/assets/252091a1-5624-4f9e-b49d-7dbaf27ef500" />

---
- `percentage` มากกว่า 85.0% ให้ส่งค่า `"DANGER (HIGH)"`

<img width="1600" height="860" alt="Picture (10)" src="https://github.com/user-attachments/assets/112fc20d-7c57-429c-b77b-fe02bd1d6dbb" />

