// ============================================================================
//  ใบงานปฏิบัติการที่ 8.5 : ศูนย์ควบคุมเซนเซอร์คู่ IoT แบบเรียลไทม์
//  รหัสนักศึกษา : 67030098
//  ----------------------------------------------------------------------------
//  การกำหนดหน้าปัดเฉพาะบุคคล (เลข 3 ตัวท้าย N = 98)
//      เกจ์ซ้าย  = (98 mod 4) + 1        = 2 + 1 = 3  ->  Retro 7-Segment
//      เกจ์ขวา   = (floor(98/4) mod 4) + 1 = (24 mod 4) + 1 = 0 + 1 = 1  ->  Analog Speedometer
//      ทั้งสองไม่ซ้ำกัน จึงไม่ต้องใช้กฎ Anti-Collision
//
//  แหล่งข้อมูล 2 ช่องสัญญาณ
//      Channel A (เกจ์ซ้าย)  : Potentiometer ฮาร์ดแวร์จริงบน ESP32 ผ่าน USB Serial
//      Channel B (เกจ์ขวา)   : สัญญาณจำลองที่สร้างจากฝั่ง C# (Sine Wave)
//                              หรือช่องที่สองจากฮาร์ดแวร์ ถ้า ESP32 ส่งมาแบบ "1234,5678"
// ============================================================================

using System.Globalization;
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<DualSerialBridgeWorker>();

var app = builder.Build();

// เสิร์ฟไฟล์หน้าเว็บจาก wwwroot และเปิด index.html เป็นหน้าแรกอัตโนมัติ
app.UseDefaultFiles();
app.UseStaticFiles();

// --- Endpoint สถานะระบบ ----------------------------------------------------
app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway",
    studentId = "67030098",
    leftGauge = "3 - Retro 7-Segment",
    rightGauge = "1 - Analog Speedometer",
    uptimeSeconds = Environment.TickCount64 / 1000,

    // รูปแบบ "o" (Round-trip) เป็น culture-independent ตามสเปกของ .NET อยู่แล้ว
    // จึงไม่เกิดปัญหาปีพุทธศักราชบนเครื่องที่ตั้งภาษาไทย
    serverTime = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
}));

// --- Endpoint ข้อมูล Telemetry 2 ช่องสัญญาณ --------------------------------
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));


// ============================================================================
//  Endpoint ควบคุมช่อง B ด้วยมือ (Control Endpoint — Query Parameters)
//  ----------------------------------------------------------------------------
//  ใช้เทคนิค Query Parameter ตามบทเรียนที่ 2.7 "การรับคำสั่งควบคุม"
//
//  ตัวอย่างการเรียกใช้
//      /api/control/channelb?mode=auto                  -> กลับไปใช้คลื่นไซน์อัตโนมัติ
//      /api/control/channelb?mode=manual&value=3000     -> ปรับค่าเองเป็น 3000
//      /api/control/channelb?value=1500                 -> เปลี่ยนเฉพาะค่า (เข้าโหมด manual ให้เอง)
//
//  พารามิเตอร์ mode และ value เป็น optional จึงประกาศเป็น string? และ int?
//  ถ้าประกาศเป็น string / int เฉยๆ แล้วผู้ใช้ไม่ส่งมา จะได้ HTTP 400 ทันที
// ============================================================================
app.MapGet("/api/control/channelb", (DualChannelStateStore state, string? mode, int? value) =>
{
    bool manual;

    if (string.IsNullOrEmpty(mode))
    {
        // ไม่ระบุโหมดแต่ส่งค่ามา = ตั้งใจจะปรับเอง จึงเข้าโหมด manual ให้อัตโนมัติ
        manual = value.HasValue || state.GetChannelBControl().manual;
    }
    else if (mode.Equals("manual", StringComparison.OrdinalIgnoreCase))
    {
        manual = true;
    }
    else if (mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
    {
        manual = false;
    }
    else
    {
        return Results.BadRequest(new
        {
            error = "พารามิเตอร์ mode ต้องเป็น 'auto' หรือ 'manual' เท่านั้น",
            received = mode
        });
    }

    state.SetChannelBControl(manual, value);

    var (currentMode, currentValue) = state.GetChannelBControl();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Channel B -> " +
                      $"{(currentMode ? "MANUAL" : "AUTO")} (value = {currentValue})");

    return Results.Ok(new
    {
        mode = currentMode ? "manual" : "auto",
        value = currentValue,
        percentage = Math.Round((currentValue / 4095.0) * 100.0, 1)
    });
});

app.Run();


// ============================================================================
//  คลังข้อมูลส่วนกลาง 2 ช่องสัญญาณ (Thread-Safe Dual-Channel State Store)
//  ----------------------------------------------------------------------------
//  Worker เขียนค่าจากเธรดเบื้องหลัง ส่วน Kestrel อ่านจากเธรดที่รับ HTTP Request
//  การล็อกครั้งเดียวแล้วอ่านทั้งสองช่องพร้อมกัน (Atomic Snapshot) รับประกันว่า
//  ค่า A และ B ที่ส่งออกไปมาจากช่วงเวลาเดียวกันเสมอ ไม่ใช่คนละรอบการอ่าน
// ============================================================================
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _source = "Initializing";
    private DateTime _lastUpdated = DateTime.UtcNow;

    // ---- สถานะการควบคุมช่อง B ด้วยมือ (Manual Override) ----
    // false = Auto   : ปล่อยให้สร้างสัญญาณจำลองเป็นคลื่นไซน์เองอัตโนมัติ
    // true  = Manual : ใช้ค่าที่ผู้ใช้ตั้งจากสไลเดอร์บนหน้าเว็บ
    private bool _manualB = false;
    private int _manualBValue = 2048;   // ค่าเริ่มต้นกลางสเกล (ประมาณ 50%)

    public void Update(int rawA, int rawB, string source)
    {
        lock (_lock)
        {
            // Clamp กันค่าเพี้ยนจากสัญญาณรบกวนไม่ให้หลุดออกนอกช่วง ADC 12 บิต
            _rawA = Math.Clamp(rawA, 0, 4095);
            _rawB = Math.Clamp(rawB, 0, 4095);
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    /// <summary>ตั้งโหมดของช่อง B และค่าที่ต้องการ (เรียกจาก Endpoint ควบคุม)</summary>
    public void SetChannelBControl(bool manual, int? value)
    {
        lock (_lock)
        {
            _manualB = manual;
            if (value.HasValue)
            {
                _manualBValue = Math.Clamp(value.Value, 0, 4095);
            }
        }
    }

    /// <summary>อ่านสถานะการควบคุมช่อง B (เรียกจาก Background Worker)</summary>
    public (bool manual, int value) GetChannelBControl()
    {
        lock (_lock)
        {
            return (_manualB, _manualBValue);
        }
    }

    public object GetSnapshot()
    {
        lock (_lock)
        {
            return new
            {
                channelA = new
                {
                    name = "Potentiometer (Hardware)",
                    gauge = "7-Segment",
                    rawValue = _rawA,
                    voltage = Math.Round((_rawA / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawA / 4095.0) * 100.0, 1)
                },
                channelB = new
                {
                    name = _manualB ? "Manual Control (Slider)" : "Secondary Sensor (Simulated)",
                    gauge = "Speedometer",
                    rawValue = _rawB,
                    voltage = Math.Round((_rawB / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawB / 4095.0) * 100.0, 1),

                    // ส่งสถานะโหมดกลับไปให้หน้าเว็บ เพื่อให้ปุ่มและสไลเดอร์
                    // แสดงสถานะตรงกับเซิร์ฟเวอร์เสมอ แม้จะเปิดหลายแท็บพร้อมกัน
                    mode = _manualB ? "manual" : "auto",
                    manualValue = _manualBValue
                },
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("o", CultureInfo.InvariantCulture)
            };
        }
    }
}


// ============================================================================
//  Background Worker : ดักฟัง Serial 2 ช่อง + ระบบจำลองสำรอง
//  ----------------------------------------------------------------------------
//  รองรับข้อมูลขาเข้า 2 รูปแบบโดยอัตโนมัติ
//     "2840"        -> ช่อง A จากฮาร์ดแวร์ + ช่อง B จำลองจากฝั่ง C#
//     "2840,1720"   -> ทั้งสองช่องมาจากฮาร์ดแวร์จริง (คะแนนพิเศษ Bonus)
// ============================================================================
public class DualSerialBridgeWorker : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;
    private readonly ILogger<DualSerialBridgeWorker> _logger;
    private readonly IConfiguration _config;

    public DualSerialBridgeWorker(
        DualChannelStateStore stateStore,
        ILogger<DualSerialBridgeWorker> logger,
        IConfiguration config)
    {
        _stateStore = stateStore;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// หาค่าของช่อง B ตามโหมดที่ผู้ใช้เลือก
    ///   โหมด Manual : คืนค่าที่ตั้งไว้จากสไลเดอร์บนหน้าเว็บ
    ///   โหมด Auto   : สร้างสัญญาณจำลองเป็นคลื่นไซน์ตามความถี่ที่กำหนด
    /// </summary>
    private (int value, string label) ResolveChannelB(double frequency)
    {
        var (manual, manualValue) = _stateStore.GetChannelBControl();

        if (manual)
        {
            return (manualValue, "Manual B");
        }

        double t = Environment.TickCount64 / 1000.0;
        int simulated = (int)((Math.Sin(t * frequency) + 1.0) / 2.0 * 4095);
        return (simulated, "Sim B");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // คืนสิทธิ์ให้ Host เริ่ม Kestrel Web Server ได้ทันที ไม่ติดบล็อกอยู่ในลูปนี้
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length > 0)
            {
                _logger.LogInformation("📋 รายการ COM Ports ในระบบ: [{Ports}]",
                    string.Join(", ", availablePorts));

                // อ่านพอร์ตเป้าหมายจาก appsettings.json — เว้นว่างไว้เพื่อให้เลือกอัตโนมัติ
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
                    selectedPort = availablePorts
                        .FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase))
                        ?? availablePorts[0];
                }

                _logger.LogInformation("🔌 กำลังทดลองเชื่อมต่อพอร์ต: {Port}", selectedPort);

                try
                {
                    using var serial = new SerialPort(selectedPort, 115200)
                    {
                        ReadTimeout = 2000,
                        DtrEnable = true,
                        RtsEnable = true
                    };
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์สำเร็จบน {Port} (Live Mode)", selectedPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            if (serial.BytesToRead > 0)
                            {
                                string line = serial.ReadLine().Trim();

                                if (line.Contains(','))
                                {
                                    // รูปแบบ 2 ช่องจากฮาร์ดแวร์จริง : "2840,1720"
                                    var parts = line.Split(',');
                                    if (parts.Length >= 2 &&
                                        int.TryParse(parts[0], out int vA) &&
                                        int.TryParse(parts[1], out int vB))
                                    {
                                        _stateStore.Update(vA, vB, $"Live Hardware A+B ({selectedPort})");
                                    }
                                }
                                else if (int.TryParse(line, out int valueA))
                                {
                                    // รูปแบบช่องเดียว : ช่อง A จริง + ช่อง B (จำลอง หรือ ปรับเอง)
                                    var (valueB, labelB) = ResolveChannelB(1.2);
                                    _stateStore.Update(valueA, valueB,
                                        $"Live Hardware A ({selectedPort}) + {labelB}");
                                }
                                // บรรทัดที่ parse ไม่ผ่าน (เช่นข้อความตอนบูตของ ESP32) จะถูกข้ามไปเฉยๆ
                            }
                            else
                            {
                                // ไม่มีข้อมูลเข้ามา -> พักเธรดสั้นๆ ไม่ให้ CPU หมุนเปล่า
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
                    // กรณีที่พบบ่อยที่สุดคือ UnauthorizedAccessException
                    // เพราะ ESP-IDF Monitor หรือโปรแกรมอื่นยังยึดพอร์ต COM อยู่
                    _logger.LogWarning("⚠️ ไม่สามารถเปิด {Port} ({Msg}) -> สลับเข้าโหมดจำลอง",
                        selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("🔍 ไม่พบพอร์ต USB -> ทำงานในโหมดจำลองทั้ง 2 ช่อง");
            }

            // ---- โหมดจำลองสำรอง (Fallback Simulation) ----
            // ใช้ Sine กับ Cos คนละความถี่ เพื่อให้ทั้งสองเกจ์ขยับไม่พร้อมกัน มองเห็นความต่างชัด
            // วน 20 รอบ (2 วินาที) ก่อนกลับไปสแกนพอร์ตใหม่ เพื่อไม่ให้ log แสดงผลรัวเกินไป
            // และทำให้เสียบสายกลับเข้าไปแล้วคืนสู่ Live Mode เองภายใน ~2 วินาที
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;
                int simA = (int)((Math.Sin(t * 0.8) + 1.0) / 2.0 * 4095);

                // ช่อง B ยังเคารพโหมด Manual แม้จะไม่ได้ต่อฮาร์ดแวร์
                // ทำให้ทดลองเลื่อนสไลเดอร์ดูได้โดยไม่ต้องมีบอร์ด
                var (simB, labelB) = ResolveChannelB(1.4);

                _stateStore.Update(simA, simB, $"Simulation Mode A + {labelB}");
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
