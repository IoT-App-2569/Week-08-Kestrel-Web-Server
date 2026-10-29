## reportLab8-1
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// กิจกรรมที่ 2: เปลี่ยนข้อความทักทายเป็นชื่อนักศึกษา
app.MapGet("/", () => "Welcome to IoT Edge Gateway by Kittikarn!");

// กิจกรรมที่ 3: การแปลง C# Object เป็น JSON อัตโนมัติ
app.MapGet("/api/status", () => new {
    gateway = "ESP32-EdgeGateway",
    status = "Online",
    uptimeSeconds = Environment.TickCount64 / 1000,
    isHealthy = true
});

// กิจกรรมที่ 4: การรับค่าผ่าน URL Path (Route Parameters) พร้อม Log บันทึกบน Terminal
app.MapGet("/api/led/{state}", (string state) => {
    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {state}");
    
    return Results.Ok(new { 
        device = "LED_D2", 
        requestedState = state, 
        actionResult = action,
        serverTime = DateTime.Now.ToString("HH:mm:ss")
    });
});

// ภารกิจท้าทาย (Micro-Challenge): /api/student
app.MapGet("/api/student", () => new {
    studentId = "67030023",
    studentName = "Kittikarn",
    faculty = "Faculty of Science and Technology, Computer Science and Digital Systems Engineering",
    targetSensor = "DHT22",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});

app.Run();
## คำถามท้ายการทดลอง (Review Questions)
1. ในสถาปัตยกรรมของ Kestrel ตัวแปร builder ทำหน้าที่อะไร และตัวแปร app ทำหน้าที่อะไร

ตัวแปร builder (WebApplicationBuilder): ทำหน้าที่จัดการเตรียมสภาพแวดล้อมเริ่มต้นของระบบ (Initialization & Configuration) ก่อนจะสร้างแอปพลิเคชัน เช่น การลงทะเบียนบริการต่างๆ (Dependency Injection), การโหลดค่าคอนฟิกจากไฟล์ตั้งค่า (appsettings.json) และการปรับแต่งค่าคอนฟิกของ Kestrel Web Server

ตัวแปร app (WebApplication): ทำหน้าที่เป็นตัวแอปพลิเคชันหลักที่สร้างขึ้นมาจาก builder มีหน้าที่จัดการเส้นทางคำขอ (Request Routing / Endpoints), กำหนดลำดับกระบวนการทำงานกลาง (Middleware Pipeline) และสั่งเริ่มต้นการทำงานของ Kestrel Web Server ผ่านคำสั่ง app.Run()


2. เปรียบเทียบความสะดวกระหว่างการสร้าง Web Server บน .NET Minimal API กับการรันผ่าน LAMP Stack (Apache + PHP) ว่ามีข้อดีข้อเสียต่างกันอย่างไรในมุมมองของงาน IoT Gateway

.NET Minimal API (Kestrel):

ข้อดี: เป็น Self-contained Web Server ในตัว ประมวลผลแบบ Asynchronous High-Performance กินทรัพยากร CPU และ RAM ต่ำ เหมาะสมอย่างยิ่งกับอุปกรณ์ IoT Edge Gateway และมีระบบแปลง C# Object เป็น JSON ให้อัตโนมัติโดยไม่ต้องใช้ฟังก์ชันเสริม

ข้อเสีย: จำเป็นต้องติดตั้ง .NET Runtime หรือคอมไพล์โปรเจกต์แยกตามสถาปัตยกรรมของฮาร์ดแวร์ก่อนนำไปใช้งาน

LAMP Stack (Apache + PHP):

ข้อดี: เป็นระบบดั้งเดิมที่หาตัวอย่างการใช้งานได้ง่าย และปรับแก้ไฟล์โค้ดสคริปต์ PHP บนเซิร์ฟเวอร์ได้ทันทีโดยไม่ต้องคอมไพล์ใหม่

ข้อเสีย: โครงสร้างซับซ้อนเพราะต้องติดตั้งบริการหลายตัว (Apache + PHP Engine) กินทรัพยากรระบบสูงกว่าจากการสร้าง Thread ประมวลผล และต้องใช้ฟังก์ชัน json_encode() / json_decode() จัดการแปลงข้อมูล JSON ด้วยตนเอง


3. นักศึกษาคิดว่าการเพิ่ม /api/ เข้าไปใน route นั้นมีประโยชน์อย่างไรบ้าง ถ้าไม่ใส่จะเกิดปัญหาอะไรบ้าง

ประโยชน์ของการใส่ /api/:

การจัดหมวดหมู่เส้นทาง (API Namespacing): ช่วยแยกแยะระหว่าง Route ที่ส่งคืนข้อมูลแบบ JSON/Data (/api/...) ออกจาก Route ที่แสดงผลหน้าเว็บเพจทั่วไป (HTML/CSS) อย่างชัดเจน

ความสะดวกในการจัดการระบบ: ง่ายต่อการตั้งค่า Reverse Proxy (เช่น Nginx), การตั้งค่าระบบรักษาความปลอดภัย (Authentication), การจำกัดอัตราส่งข้อมูล (Rate Limiting) และการกำหนดสิทธิ์การเข้าถึงข้อมูล (CORS)

รองรับการทำ API Versioning: สามารถต่อยอดเป็น /api/v1/... หรือ /api/v2/... ในอนาคตได้ง่าย

ปัญหาที่อาจเกิดขึ้นถ้าไม่ใส่ /api/:

เกิดการชนกันของ Route (Route Collision): ชื่อเอนด์พอยต์อาจไปซ้ำกับชื่อโฟลเดอร์ หรือไฟล์ Static Web เช่น HTML/CSS/JS ได้

เกิดความสับสนในการพัฒนา: ผู้พัฒนาร่วมหรือระบบภายนอกจะไม่ทราบแน่ชัดว่า Endpoint นั้นส่งคืนค่าเป็นโครงสร้างข้อมูล (API Data) หรือเป็นหน้าจอสำหรับแสดงผล (UI)