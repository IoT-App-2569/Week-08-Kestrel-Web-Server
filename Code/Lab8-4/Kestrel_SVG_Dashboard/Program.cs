// ============================================================================
//  ใบงานการทดลองที่ 8.4 : แดชบอร์ดมาตรวัดความเร็ว SVG
//  รหัสนักศึกษา : 67030098
//  ----------------------------------------------------------------------------
//  ต่อยอดจากใบงานที่ 8.3 โดยเพิ่มการเสิร์ฟไฟล์หน้าเว็บจากโฟลเดอร์ wwwroot
//
//   ESP32 --USB--> [SerialBridgeWorker] --> [TelemetryStateStore] --> [/api/telemetry]
//                                                                            |
//                                                     fetch() ทุก 150 ms     v
//                                            [wwwroot/index.html : SVG Speedometer]
// ============================================================================

using System.Globalization;
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน StateStore เป็น Singleton (มีชิ้นเดียวตลอดอายุโปรแกรม)
// ทั้ง Worker และ Endpoint จะได้อ้างถึงออบเจกต์ตัวเดียวกัน
builder.Services.AddSingleton<TelemetryStateStore>();

// ลงทะเบียน Worker เป็น Background Hosted Service
// .NET จะเรียก ExecuteAsync() ให้อัตโนมัติตอนแอปเริ่มทำงาน
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// --- เปิดใช้งานการเสิร์ฟไฟล์สถิต (HTML, CSS, JS, SVG) จากโฟลเดอร์ wwwroot ---
// UseFileServer() = UseDefaultFiles() + UseStaticFiles()
// ทำให้เปิด http://localhost:5000 แล้วได้ wwwroot/index.html โดยอัตโนมัติ
//
// สังเกตว่าไม่มี app.MapGet("/") อีกต่อไป เพราะหน้าแรกถูกแทนที่ด้วย index.html
// นี่คือเหตุผลที่ endpoint ข้อมูลต้องมี prefix "/api/" เพื่อไม่ให้ชนกับไฟล์ใน wwwroot
app.UseFileServer();


// --- Endpoint ดึงค่า Telemetry ล่าสุด ---------------------------------------
// TelemetryStateStore ถูกฉีดเข้ามาให้อัตโนมัติผ่าน Dependency Injection
app.MapGet("/api/telemetry", (TelemetryStateStore state) =>
{
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();

    // ----- ภารกิจท้าทาย (Micro-Challenge) : จัดระดับการแจ้งเตือน -----
    // โจทย์ : > 85% = DANGER (HIGH) | 70 - 85% = WARNING | < 70% = NORMAL
    // switch expression ตรวจเงื่อนไขจากบนลงล่างและหยุดที่เงื่อนไขแรกที่เป็นจริง
    // ที่ 85.0 พอดีจะได้ WARNING (ไม่ใช่ DANGER) เพราะเงื่อนไขแรกใช้ "มากกว่า" 85.0
    string alertLevel = percent switch
    {
        > 85.0 => "DANGER (HIGH)",
        >= 70.0 => "WARNING",
        _ => "NORMAL"
    };

    return Results.Ok(new
    {
        sensor = "ESP32-Potentiometer",
        rawValue = raw,
        voltage = voltage,
        percentage = percent,
        alertLevel = alertLevel,
        dataSource = source,
        // ต้องระบุ InvariantCulture เสมอ มิฉะนั้นบนเครื่องที่ตั้งภาษาไทย (th-TH)
        // ปฏิทินเริ่มต้นจะเป็นพุทธศักราช ทำให้ได้ปี 2569 แทนที่จะเป็น 2026
        // ซึ่งผิดรูปแบบมาตรฐาน ISO 8601 และฝั่ง JavaScript จะ parse ไม่ได้
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
    });
});

app.Run();


// ============================================================================
//  1. THREAD-SAFE STATE STORE (คลังเก็บค่าสถานะกลาง)
//  ----------------------------------------------------------------------------
//  Worker เขียนค่าจากเธรดเบื้องหลัง ส่วน Kestrel อ่านค่าจากเธรดที่รับ HTTP Request
//  ถ้าไม่ล็อกอาจเกิดการอ่านค่าขณะกำลังเขียนค้างอยู่ (Torn Read) ข้อมูลจะไม่สอดคล้องกัน
// ============================================================================
public class TelemetryStateStore
{
    private readonly object _lock = new();
    private int _rawValue = 0;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private string _source = "Initializing";

    /// <summary>บันทึกค่าล่าสุดที่อ่านได้ (เรียกจากเธรดของ Worker)</summary>
    public void Update(int rawValue, string source)
    {
        lock (_lock)
        {
            _rawValue = rawValue;
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// อ่านค่าทั้งชุดออกมาพร้อมกันในการล็อกครั้งเดียว (Atomic Snapshot)
    /// เพื่อรับประกันว่า raw, voltage, percent ที่ได้มาจากช่วงเวลาเดียวกันเสมอ
    /// </summary>
    public (int raw, double voltage, double percent, string source, DateTime updated) GetSnapshot()
    {
        lock (_lock)
        {
            double voltage = Math.Round((_rawValue / 4095.0) * 3.3, 2);
            double percent = Math.Round((_rawValue / 4095.0) * 100.0, 1);
            return (_rawValue, voltage, percent, _source, _lastUpdated);
        }
    }
}


// ============================================================================
//  2. BACKGROUND WORKER (คนงานดักฟังข้อมูลจากสาย USB)
//  ----------------------------------------------------------------------------
//  สถาปัตยกรรม Dual-Mode : พยายามต่อฮาร์ดแวร์จริงก่อน ถ้าไม่สำเร็จให้สลับไป
//  โหมดจำลองอัตโนมัติ (Graceful Degradation) เว็บเซิร์ฟเวอร์จึงไม่มีวันล่ม
//  แม้จะถอดสาย USB ออกกลางคัน
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;
    private readonly IConfiguration _config;

    public SerialBridgeWorker(
        TelemetryStateStore stateStore,
        ILogger<SerialBridgeWorker> logger,
        IConfiguration config)
    {
        _stateStore = stateStore;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // คืนสิทธิ์ให้ Host เริ่ม Kestrel Web Server ได้อย่างราบรื่น ไม่ติดบล็อก
        // ถ้าไม่มีบรรทัดนี้ เว็บเซิร์ฟเวอร์จะไม่เริ่มทำงานจนกว่าลูปข้างล่างจะ await ครั้งแรก
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            // ---- 1. ค้นหาพอร์ต COM ทั้งหมดที่มีอยู่ในเครื่อง ----
            string[] availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length > 0)
            {
                _logger.LogInformation("📋 รายการ COM Ports ในระบบ: [{Ports}]",
                    string.Join(", ", availablePorts));

                // อ่านพอร์ตเป้าหมายจาก appsettings.json (คีย์ "SerialPort:Port")
                // เว้นว่างไว้ = ให้ระบบเลือกพอร์ตแรกที่ไม่ใช่ COM1 ให้อัตโนมัติ
                string? targetPort = _config["SerialPort:Port"];

                string selectedPort;
                if (!string.IsNullOrEmpty(targetPort) &&
                    availablePorts.Contains(targetPort, StringComparer.OrdinalIgnoreCase))
                {
                    selectedPort = targetPort;
                }
                else
                {
                    if (!string.IsNullOrEmpty(targetPort))
                    {
                        _logger.LogWarning("⚠️ ไม่พบพอร์ต {Target} ในระบบ! ระบบจะเลือกพอร์ตอื่นให้อัตโนมัติ...",
                            targetPort);
                    }
                    // COM1 มักเป็นพอร์ตอนุกรมของเมนบอร์ดที่ไม่มีอุปกรณ์ต่ออยู่ จึงข้ามไปก่อน
                    selectedPort = availablePorts
                        .FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase))
                        ?? availablePorts[0];
                }

                _logger.LogInformation("🔌 กำลังทดลองเชื่อมต่อพอร์ต: {Port}", selectedPort);

                try
                {
                    // Baud rate ต้องตรงกับฝั่ง ESP32 คือ 115200 bps
                    using var serial = new SerialPort(selectedPort, 115200);
                    serial.ReadTimeout = 2000;
                    serial.Open();
                    serial.DiscardInBuffer();   // เคลียร์ข้อมูลค้างเก่าในบัฟเฟอร์
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์สำเร็จบน {Port} (Live Mode)", selectedPort);

                    // ---- ลูปอ่านข้อมูลจริง ----
                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            if (serial.BytesToRead > 0)
                            {
                                string line = serial.ReadLine().Trim();

                                // TryParse ป้องกันข้อความขยะที่ ESP32 พิมพ์ตอนบูต
                                // (เช่น "[SYSTEM] ADC Initialized...") ไม่ให้ทำโปรแกรมพัง
                                if (int.TryParse(line, out int val))
                                {
                                    _stateStore.Update(val, $"Live Hardware ({selectedPort})");
                                }
                            }
                            else
                            {
                                // ไม่มีข้อมูล -> พักเธรดสั้นๆ ไม่ให้ CPU หมุนเปล่า (Busy-Wait)
                                await Task.Delay(50, stoppingToken);
                            }
                        }
                        catch (TimeoutException)
                        {
                            await Task.Delay(50, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // กรณีที่พบบ่อยที่สุด : UnauthorizedAccessException
                    // เพราะ ESP-IDF Monitor หรือ Arduino Serial Monitor ยังยึดพอร์ตอยู่
                    _logger.LogWarning("⚠️ ไม่สามารถเปิด {Port} ({Msg}) -> สลับเข้าโหมดจำลอง",
                        selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("🔍 ไม่พบพอร์ต USB -> ทำงานในโหมดจำลอง (Simulation Mode)");
            }

            // ---- 2. โหมดจำลองสัญญาณอัตโนมัติ (Fallback Simulation Mode) ----
            // วนจำลองสัญญาณ 2 วินาที (20 รอบ x 100 ms) ก่อนกลับไปตรวจเช็คพอร์ตใหม่
            // ทำให้เสียบสายกลับเข้าไปแล้วระบบคืนสู่ Live Mode เองภายใน ~2 วินาที
            // และ log ไม่แสดงผลรัวจนอ่านไม่ทัน
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;

                // Math.Sin ให้ค่า -1 ถึง +1 -> (sin + 1) / 2 ปรับเป็น 0 ถึง 1 -> คูณ 4095
                // ความถี่เชิงมุม 1.5 rad/s -> คาบการแกว่ง = 2π / 1.5 ≈ 4.19 วินาทีต่อรอบ
                int simAdc = (int)((Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);

                _stateStore.Update(simAdc, "Simulation Mode (Sine Wave)");
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
