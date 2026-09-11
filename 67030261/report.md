# ใบงานการทดลองที่ 8.1
#### [Checkpoint 1.1 ทดสอบความเข้าใจ]
1. ในหน้าจอ Terminal ขณะที่เซิร์ฟเวอร์กำลังรันอยู่ ให้กดปุ่ม `Ctrl + C` เพื่อหยุดโปรแกรม
2. กลับไปที่หน้าเบราว์เซอร์แล้วกดปุ่ม **Refresh (F5)** สังเกตว่าเกิดอะไรขึ้น และอธิบายสั้นๆ ว่าทำไมจึงเป็นเช่นนั้น
   - **คำตอบ** ไม่สามารถเปิดได้ เพราะ หยุดรัน server ไปแล้ว
#### กิจกรรมที่ 2 โครงสร้างและเขียนโค้ด
<img width="705" height="191" alt="image" src="https://github.com/user-attachments/assets/c6a76a8e-eb44-40ef-a861-9eff6023e280" />

## ภารกิจท้าทาย (Micro-Challenge)
 <img width="694" height="274" alt="image" src="https://github.com/user-attachments/assets/cb0118c6-d3b9-4907-8467-8c091261a390" />

## คำถามท้ายการทดลอง (Review Questions)
1. ในสถาปัตยกรรมของ Kestrel ตัวแปร `builder` ทำหน้าที่อะไร และตัวแปร `app` ทำหน้าที่อะไร
  - builder คือตัวตั้งค่าแอปก่อน build ใช้ลงทะเบียน service, configuration, logging, ตั้งค่า Kestrel 
  - app คือแอปที่ build เสร็จเเล้วพร้อมรัน ใช้กำหนด middleware, route และสั่งเริ่มเซิร์ฟเวอร์ด้วย app.Run()
2. เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway
  - Minimal API บน Kestrel เซตอัปเร็ว โค้ดน้อย เป็น async เบา เหมาะกับอุปกรณ์ทรัพยากรจำกัด จัดการ connection พร้อมกันจากหลายเซนเซอร์ได้ดี เชื่อมกับ protocol อื่นเช่น Serial, BLE, MQTT ในโค้ดเดียวกันได้สะดวก
  - LAMP ต้องติดตั้งและ config Apache แยกจากโค้ด PHP กิน resource ต่อ request มากกว่า ไม่เหมาะกับงาน real time เท่า แต่คุ้นเคยง่ายกว่าสำหรับงานเว็บทั่วไปและมี community ใหญ่กว่า
3. นักศึกษาคิดว่าการเพิ่ม `/api/` เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง
  - ถ้าใส่ /api/ แยกชัดระหว่าง endpoint ที่เป็นข้อมูล กับหน้าเว็บหรือไฟล์ static ตั้ง middleware, auth, หรือ rate limit เฉพาะกลุ่ม /api/ ได้ง่าย รองรับการทำ reverse proxy หรือแยก backend ในอนาคต กันไม่ให้ route ชนกับชื่อไฟล์ static 
  - ถ้าไม่ใส่ /api/ อาจเกิดปัญหา route ชนกับไฟล์ static ที่ชื่อเดียวกัน แยกยากว่า endpoint ไหนคืนข้อมูล endpoint ไหนคืนหน้าเว็บ ตั้ง security หรือ middleware เฉพาะกลุ่มยากขึ้น ขยายระบบภายหลัง เช่นทำ versioning /api/v1/ ทำได้ลำบากกว่า

---

# ใบงานการทดลองที่ 8.2

#### [Checkpoint 2.1 ทดสอบความเข้าใจ]
1. ใน ESP-IDF v6.x ฟังก์ชัน `adc_oneshot_read()` คืนค่าผลลัพธ์เป็นตัวเลขแบบใด และมีช่วงค่าต่ำสุด-สูงสุดเท่าใดเมื่อตั้งค่าเป็น `ADC_BITWIDTH_12`
   - **คำตอบ:** adc_oneshot_read คืนค่าเป็นจำนวนเต็ม int ผ่าน pointer raw_val เมื่อตั้ง ADC_BITWIDTH_12 ช่วงค่าคือ 0 ถึง 4095
1. ทำไมเราจึงต้องกดปุ่ม `Ctrl + ]` เพื่อปิด **ESP-IDF Monitor** ก่อนที่เราจะรันโปรแกรมฝั่ง C# Kestrel ในใบงานถัดไป
   *(คำใบ้: หากโปรเซส idf.py monitor ยังเปิดค้างอยู่ จะเกิดอะไรขึ้นกับพอร์ต COM)*
   - **คำตอบ:** ต้องกด Ctrl + ] ปิด Monitor ก่อน เพราะโปรเซส idf.py monitor จะจับพอร์ต COM ไว้ใช้งานอยู่ ถ้าไม่ปิดจะล็อกพอร์ตค้าง ทำให้โปรแกรม C# ฝั่ง Kestrel เปิดพอร์ต COM เดียวกันเพื่ออ่าน serial ไม่ได้ ต้องปลดล็อกก่อนถึงจะใช้พอร์ตนั้นต่อได้

---

## ภารกิจท้าทาย (Micro-Challenge) (ประสบการณ์จากใบงาน LDR)
- ให้นักศึกษาทดลองเปลี่ยนตัวต้านทานปรับค่าได้เป็น **LDR (Light Dependent Resistor)** ร่วมกับตัวต้านทาน $10\text{ k}\Omega$ แบ่งแรงดัน
- ทดลองใช้ไฟฉายจากโทรศัพท์มือถือส่อง และเอามือปิดบังแสง สังเกตค่า ADC บนหน้าจอเทอร์มินัลว่าเปลี่ยนแปลงอย่างไร
```
        ใช้ไฟจากมือถือ ADC จะเพิ่ม เเต่ถ้าเอามือบัง ADC จะลดลง ยิ่งสว่าง ADC ยิ่งเพิ่มเเต่ถ้ามันมืด ADC จะลด
```
- บันทึกภาพถ่ายการต่อวงจรและภาพหน้าจอ Monitor ลงในรายงานผลการทดลอง

<img width="3024" height="4032" alt="IMG_4051" src="https://github.com/user-attachments/assets/8e1f9968-2c8d-41fc-a181-574995641f1d" />

<img width="727" height="315" alt="image" src="https://github.com/user-attachments/assets/a6ac44c5-f847-4872-81cd-517365c4b103" />

---

# ใบงานการทดลองที่ 8.3
### 🌟 กิจกรรมที่ 5: ทดสอบการทำงานสดๆ (The Magic Moment)
<img width="602" height="268" alt="image" src="https://github.com/user-attachments/assets/bb1fbe21-8f4f-435f-b32f-310474acec0d" />


#### 📋 [Checkpoint 3.1: ทดสอบกลไก Fallback Simulation]
1. ขณะที่โปรแกรม C# กำลังรันอยู่ ให้ถอดสาย USB ของ ESP32 ออกจากคอมพิวเตอร์
2. กด Refresh (F5) บนหน้าเบราว์เซอร์หลายๆ ครั้งติดต่อกัน สังเกตว่า:
   - ค่า `dataSource` เปลี่ยนเป็นอะไร?
   - ค่า `rawValue` ยังเปลี่ยนได้อยู่หรือไม่ และเปลี่ยนในลักษณะใด?
   - **คำตอบ:** dataSource เป็น "Live Hardware (/dev/tty.usbserial-0001)" ส่วน rawValue เป็น 1922 ไม่เปลียนเลย

## 🎯 ภารกิจท้าทาย (Micro-Challenge)
- ให้นักศึกษาเพิ่มฟิลด์ `alertLevel` เข้าไปในผลลัพธ์ JSON ของ `/api/telemetry`:
  - ถ้า `percentage` มากกว่า 85.0% ให้ส่งค่า `"DANGER (HIGH)"`
  - ถ้า `percentage` ระหว่าง 70.0% - 85.0% ให้ส่งค่า `"WARNING"`
  - ถ้า `percentage` ต่ำกว่า 70.0% ให้ส่งค่า `"NORMAL"`
- บันทึกภาพหน้าจอเบราว์เซอร์ขณะหมุนไปที่ระดับต่างๆ เพื่อแสดงว่าฟิลด์ `alertLevel` ทำงานถูกต้อง

### NORMAL
<img width="479" height="289" alt="image" src="https://github.com/user-attachments/assets/fd19acb7-97c3-4035-9569-5779be2b2adf" />

---

## WARNING
<img width="528" height="289" alt="image" src="https://github.com/user-attachments/assets/155a280c-41b5-4cc6-ab3c-8fd173bb0a9a" />

---

## DANGER
<img width="580" height="281" alt="image" src="https://github.com/user-attachments/assets/c2d81485-807b-4f5e-81f7-e033c40d7722" />

---

# ใบงานการทดลองที่ 8.4 (Labsheet 8.4)


## 📋 ส่งงานและประเมินผล (Submission Check)
1. บันทึกวิดีโอคลิปสั้น (15-30 วินาที) โดยในคลิปต้องเห็น:
   - นิ้วมือนักศึกษากำลังหมุนตัวต้านทานปรับค่าได้บนบอร์ด ESP32
   - หน้าจอคอมพิวเตอร์ที่เข็มไมล์ Speedometer / VU Meter กวาดตามมืออย่างชัดเจน

https://github.com/user-attachments/assets/41a12657-4c94-48ea-a3b3-c778cf192c47

2. แนบภาพหน้าจอซอร์สโค้ดและรายงานการทดลอง

<img width="1512" height="950" alt="image" src="https://github.com/user-attachments/assets/95d64ac7-e0c7-40ce-a6db-def139fbceca" />

# ใบงานการทดลองที่ 8.5

## ส่งงานและประเมินผล (Submission & Grading Rubric)

### สิ่งที่ต้องส่ง
1. **คลิปวิดีโอสาธิตการทำงาน (15 - 30 วินาที)**
   * ถ่ายให้เห็น **รหัสนักศึกษา** และผลการคำนวณคู่เกจ์
   * นิ้วมือหมุน Potentiometer บนบอร์ด ESP32 จริง แล้วเกจ์ฝั่งซ้ายกวาดตามมืออย่างชัดเจน
   * เกจ์ฝั่งขวาขยับตามข้อมูลช่อง B (Simulation หรือ เซนเซอร์ตัวที่ 2)


https://github.com/user-attachments/assets/6d1aef8f-bc0c-4fa6-9719-f1ea3f75e5bb


2. **รายงาน (markdown/pull request) สรุปผลการทดลอง**
   * ระบุเลขรหัสนักศึกษาและแสดงวิธีคำนวณหาเกจ์ซ้าย-ขวา
   * ภาพหน้าจอแดชบอร์ดที่ทำงานสมบูรณ์
   * อธิบายหลักการทำงานของฟังก์ชัน JavaScript ในการเชื่อมต่อข้อมูล

## หลักการทำงานของ JavaScript ในการเชื่อมต่อข้อมูล (Fetch API)

การเชื่อมต่อข้อมูลกับ Server ผ่าน JavaScript หลักๆใช้ `fetch()` ทำงานเเบบ **Asynchronous** (ไม่รอ) โดยมีขั้นตอนดังนี้

```javascript
fetch('/api/data')          // 1. ส่ง Request ไปที่ Server
  .then(res => res.json())  // 2. รอ Response แล้วแปลงเป็น JSON
  .then(data => {           // 3. เอาข้อมูลมาใช้ เช่น อัพเดตหน้าเว็บ
    console.log(data);
  })
  .catch(err => console.log(err)); // 4. ดักจับ Error ถ้าเชื่อมไม่ติด
```

### หลักการทำงาน
1. `fetch()` จะยิง Request ออกไปหา Server (เช่น ESP32/Kestrel) แบบไม่บล็อกโค้ดส่วนอื่น
2. ได้ `Promise` กลับมาก่อน แล้วรอ Server ตอบ (Response) กลับมาทีหลัง
3. เมื่อ Server ตอบมา จะแปลงข้อมูล (มักเป็น JSON หรือ Text) ผ่าน `.then()`
4. ถ้าอยากดึงข้อมูลซ้ำๆ (real-time) มักใช้ `setInterval()` ควบคู่กับ fetch

```javascript
setInterval(() => {
  fetch('/api/sensor').then(res => res.text()).then(val => {
    document.getElementById("temp").innerText = val;
  });
}, 1000); // ดึงข้อมูลทุก 1 วิ
```

## สรุปผลการทดลอง
   การทดลองนี้แสดงให้เห็นการต่อ Sensor 2 ตัว → Serial → Server → Dashboard แบบขนานกันจริงๆ ไม่ใช่แค่ตัวเดียว จุดเด่นคือระบบทนต่อความผิดพลาดได้ดี เพราะถ้าฮาร์ดแวร์หลุดหรือหาพอร์ตไม่เจอ ตัว Server จะไม่ค้างหรือ error แต่จะปั้นข้อมูลจำลองมาแสดงแทนอัตโนมัติ ทำให้ Dashboard ทำงานต่อได้เสมอ ถือว่าเป็นการฝึกทั้งเรื่อง multi-channel ADC, background service แบบ async, และการอัพเดต UI แบบ real-time ผ่าน polling ไปพร้อมกันในโปรเจกต์เดียว
