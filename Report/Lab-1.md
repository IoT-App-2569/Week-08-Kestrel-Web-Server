# รายงานผลการทดลองที่ 8.1: พื้นฐาน Kestrel Web Server และ Minimal API

**ชื่อ-นามสกุล:** นาย พรพรม สุขใจมิตร

**รหัสนักศึกษา:** 67030323

---

## [Checkpoint 1.1 ทดสอบความเข้าใจ]

**คำถาม:** ในหน้าจอ Terminal ขณะที่เซิร์ฟเวอร์กำลังรันอยู่ ให้กดปุ่ม `Ctrl + C` เพื่อหยุดโปรแกรม กลับไปที่หน้าเบราว์เซอร์แล้วกดปุ่ม Refresh (F5) สังเกตว่าเกิดอะไรขึ้น และอธิบายสั้นๆ ว่าทำไมจึงเป็นเช่นนั้น

**คำตอบ:** เมื่อกด Refresh หน้าเว็บจะแสดงผลข้อผิดพลาด This site can’t be reached สาเหตุเป็นเพราะการกด `Ctrl + C` คือการสั่งปิดการทำงานของ Kestrel Web Server ทำให้ไม่มีโปรเซสใดๆ คอยรอรับ  และตอบสนองต่อ Request จากเบราว์เซอร์อีกต่อไป

---

## กิจกรรมที่ 2: โครงสร้างและเขียนโค้ด

*(แนบภาพหน้าจอเบราว์เซอร์ที่แสดงผลข้อความทักทายพร้อมชื่อ-นามสกุลของตนเอง)*

![alt text](/Report/img/image.png)

---

## กิจกรรมที่ 3: การแปลง C# Object เป็น JSON โดยอัตโนมัติ

*(แนบภาพหน้าจอเบราว์เซอร์ที่แสดงผลลัพธ์เป็น JSON จาก `/api/status`)*

![alt text](/Report/img/image-1.png)

---

## กิจกรรมที่ 4: การรับค่าผ่าน URL Path (Route Parameters)

*(แนบภาพหน้าจอเบราว์เซอร์ที่แสดงผลลัพธ์การสั่ง `on` และ `off`)*

![alt text](/Report/img/image-2.png)

![alt text](/Report/img/image-3.png)

**ผลลัพธ์การทำงานที่แสดงบน Terminal:**

```text
{"device":"LED_D2","requestedState":"on","actionResult":"TURN ON \uD83D\uDCA1","serverTime":"09:30:15"}
{"device":"LED_D2","requestedState":"off","actionResult":"TURN OFF \uD83C\uDF11","serverTime":"09:31:10"}
{"device":"LED_D2","requestedState":"pornprom_sukjaimit","actionResult":"TURN OFF \uD83C\uDF11","serverTime":"09:33:29"}
```

---

## ภารกิจท้าทาย (Micro-Challenge)

**โค้ด Endpoint** **`/api/student`** **ที่เขียนเพิ่มใน** **`Program.cs`****:**

C#

```
app.MapGet("/api/student", () => new {
    studentId = "ใส่รหัสนักศึกษาของคุณ",
    studentName = "ใส่ชื่อ-นามสกุลภาษาอังกฤษของคุณ",
    faculty = "Computer Education",
    targetSensor = "Relay Module",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});
```

*(แนบภาพหน้าจอเบราว์เซอร์ที่เปิดแสดงผล JSON จาก* *`/api/student`**)*

![alt text](/Report/img/image-4.png)

![alt text](/Report/img/image-5.png)

---

## คำถามท้ายการทดลอง (Review Questions)

**1. ในสถาปัตยกรรมของ Kestrel ตัวแปร** **`builder`** **ทำหน้าที่อะไร และตัวแปร** **`app`** **ทำหน้าที่อะไร**

**คำตอบ:**

* **`builder`** **(WebApplicationBuilder):** ทำหน้าที่จัดเตรียมสภาพแวดล้อม (Environment) ตั้งค่าการทำงานเบื้องหลัง โหลด Configuration และนำเข้า Service ต่างๆ ที่จำเป็นก่อนที่จะสร้างเซิร์ฟเวอร์
* **`app`** **(WebApplication):** ทำหน้าที่เป็นตัวเซิร์ฟเวอร์หลัก (Pipeline) ที่ใช้จัดการ Routing (เช่น คำสั่ง `MapGet`) และคอยรับ Request จากผู้ใช้งานเพื่อประมวลผลและส่ง Response กลับไป

---

**2. เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway**

**คำตอบ:**

ในมุมมองของงาน IoT Gateway การใช้ .NET Minimal API มีความสะดวกกว่า เพราะมีน้ำหนักเบา สามารถคอมไพล์เป็นโปรแกรมเดียวได้ และมี Kestrel เซิร์ฟเวอร์ในตัว สามารถนำไปรันบนอุปกรณ์ Gateway ขนาดเล็กได้ทันทีโดยใช้ทรัพยากรน้อย ต่างจาก LAMP Stack ที่ต้องติดตั้ง Service หลายตัว ซึ่งซับซ้อน ใช้ทรัพยากรระบบมากกว่า และดูแลรักษายากกว่าในสเกลของอุปกรณ์ IoT

---

**3. นักศึกษาคิดว่าการเพิ่ม** **`/api/`** **เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง**

**คำตอบ:**

การใส่ `/api/` มีประโยชน์ในการแบ่งแยกหมวดหมู่การทำงานอย่างชัดเจน ทำให้ระบบทราบว่า URL นี้มีไว้สำหรับส่งข้อมูลดิบ ให้กับอุปกรณ์หรือบอทอ่าน ไม่ใช่สำหรับแสดงผลหน้าเว็บเพจ หากไม่ใส่ อาจเกิดปัญหาชื่อ Route ซ้ำซ้อน นอกจากนี้ยังทำให้ยากต่อการจัดการเวอร์ชันของ API ในอนาคตด้วย
