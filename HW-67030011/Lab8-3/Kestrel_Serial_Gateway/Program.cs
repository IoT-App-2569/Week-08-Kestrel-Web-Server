// ============================================================================
// ใบงานที่ 8.3: Hardware Serial Bridge to Web (Kestrel_Serial_Gateway)
// นักศึกษา: 67030011 กฤตนัย บุญน้อย
// ============================================================================
// หมายเหตุ: คำสั่ง using ต้องอยู่บนสุดของไฟล์ (ก่อน Top-level statements)
// ถ้าวางไว้กลางไฟล์ตามโค้ดในใบงาน จะคอมไพล์ไม่ผ่าน (CS1529)
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน StateStore เป็น Singleton (มีชิ้นเดียวตลอดอายุโปรแกรม)
builder.Services.AddSingleton<TelemetryStateStore>();

// ลงทะเบียน SerialBridgeWorker เป็น Background Hosted Service
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// Endpoint หน้าแรก
app.MapGet("/", () => "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

// Endpoint สำหรับดึงค่า Telemetry ล่าสุด
app.MapGet("/api/telemetry", (TelemetryStateStore state) => {
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();
    return Results.Ok(new {
        sensor = "ESP32-Potentiometer",
        rawValue = raw,
        voltage = voltage,
        percentage = percent,
        // Micro-Challenge: ระดับการแจ้งเตือนตามเปอร์เซ็นต์
        alertLevel = AlertRules.FromPercent(percent),
        dataSource = source,
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture)
    });
});

app.Run();

// ============================================================================
// MICRO-CHALLENGE: กฎการแจ้งเตือน (alertLevel)
// ============================================================================
public static class AlertRules
{
    public static string FromPercent(double percent) => percent switch
    {
        > 85.0 => "DANGER (HIGH)",
        >= 70.0 => "WARNING",
        _ => "NORMAL"
    };
}

// ============================================================================
// 1. THREAD-SAFE STATE STORE (คลังเก็บค่าสถานะกลาง)
// ============================================================================
public class TelemetryStateStore
{
    private readonly object _lock = new();
    private int _rawValue = 0;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private string _source = "Initializing";

    public void Update(int rawValue, string source)
    {
        lock (_lock)
        {
            _rawValue = rawValue;
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

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
// 2. การเลือกพอร์ต Serial ให้ทำงานได้ทั้ง Windows และ macOS / Linux
// ============================================================================
public static class SerialPortPicker
{
    // ชื่อพอร์ตของชิป USB-to-UART ที่ใช้บนบอร์ด ESP32 (CP210x, CH340, FTDI, USB-JTAG)
    private static readonly string[] UsbHints = { "usbserial", "usbmodem", "SLAB_USBtoUART", "wchusbserial", "ttyUSB", "ttyACM" };

    public static string? Pick(string[] availablePorts, string? targetPort)
    {
        // 1) ถ้ากำหนดพอร์ตไว้ และมีอยู่จริงในระบบ ใช้พอร์ตนั้นเลย
        //    (บน macOS/Linux เช็คไฟล์อุปกรณ์ด้วย เพราะ GetPortNames() ไม่แสดง /dev/ttys* หรือ pty)
        if (!string.IsNullOrEmpty(targetPort) &&
            (availablePorts.Contains(targetPort, StringComparer.OrdinalIgnoreCase) || (!OperatingSystem.IsWindows() && File.Exists(targetPort))))
            return targetPort;

        // 2) Windows: เลือกพอร์ตแรกที่ไม่ใช่ COM1 (ตามใบงาน)
        if (OperatingSystem.IsWindows())
            return availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase));

        // 3) macOS / Linux: GetPortNames() คืนพอร์ต Bluetooth (เช่น /dev/cu.Bluetooth-Incoming-Port) มาด้วย
        //    จึงต้องกรองเฉพาะพอร์ต USB และบน macOS ต้องใช้ /dev/cu.* ไม่ใช่ /dev/tty.*
        //    เพราะ /dev/tty.* จะบล็อกรอสัญญาณ DCD ตอน Open
        return availablePorts
            .Where(p => UsbHints.Any(h => p.Contains(h, StringComparison.OrdinalIgnoreCase)))
            .Where(p => !OperatingSystem.IsMacOS() || p.StartsWith("/dev/cu.", StringComparison.Ordinal))
            .FirstOrDefault();
    }
}

// ============================================================================
// 3. BACKGROUND WORKER (คนงานดักฟังข้อมูลจากสาย USB)
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;
    private readonly string? _targetPort;
    private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(3);

    public SerialBridgeWorker(TelemetryStateStore stateStore, ILogger<SerialBridgeWorker> logger, IConfiguration config)
    {
        _stateStore = stateStore;
        _logger = logger;
        // ระบุพอร์ตได้จาก appsettings.json หรือ command line เช่น --Serial:Port=COM24
        _targetPort = config["Serial:Port"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // คืนสิทธิ์ให้ Host สามารถเริ่ม Kestrel Web Server ได้อย่างราบรื่น ไม่ติดบล็อก
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. ค้นหาพอร์ต COM ทั้งหมดที่มีอยู่ในเครื่อง
            string[] availablePorts = SerialPort.GetPortNames();
            string? selectedPort = SerialPortPicker.Pick(availablePorts, _targetPort);

            if (selectedPort is not null)
            {
                _logger.LogInformation("📋 รายการ COM Ports ในระบบ: [{Ports}]", string.Join(", ", availablePorts));
                _logger.LogInformation("🔌 กำลังทดลองเชื่อมต่อพอร์ต: {Port}", selectedPort);

                try
                {
                    using var serial = new SerialPort(selectedPort, 115200);
                    serial.ReadTimeout = 2000;
                    serial.Open();
                    serial.DiscardInBuffer(); // เคลียร์ขยะเก่าในบัฟเฟอร์
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์สำเร็จบน {Port} (Live Mode)", selectedPort);

                    // Watchdog: ถ้า OS ไม่โยน Exception ตอนสายหลุด ลูปนี้จะค้างค่าเดิมไว้ตลอด
                    // จึงนับเวลาจากข้อมูลล่าสุด ถ้าเงียบเกิน DataTimeout ให้ถือว่าสายหลุดแล้วสลับเข้าโหมดจำลอง
                    var lastDataAt = DateTime.UtcNow;

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        if (DateTime.UtcNow - lastDataAt > DataTimeout)
                        {
                            _logger.LogWarning("⚠️ ไม่มีข้อมูลจาก {Port} เกิน {Sec} วินาที (สายหลุด/บอร์ดค้าง?) -> สลับเข้าโหมดจำลอง", selectedPort, DataTimeout.TotalSeconds);
                            break;
                        }

                        try
                        {
                            if (serial.BytesToRead > 0)
                            {
                                string line = serial.ReadLine().Trim();
                                if (int.TryParse(line, out int val))
                                {
                                    _stateStore.Update(val, $"Live Hardware ({selectedPort})");
                                    lastDataAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                await Task.Delay(50, stoppingToken);
                            }
                        }
                        catch (TimeoutException)
                        {
                            await Task.Delay(50, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ ไม่สามารถเปิด {Port} ({Msg}) -> สลับเข้าโหมดจำลอง", selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("🔍 ไม่พบพอร์ต USB -> ทำงานในโหมดจำลอง (Simulation Mode)");
            }

            // 2. โหมดจำลองสัญญาณอัตโนมัติ (Fallback Simulation Mode)
            // วนจำลองสัญญาณ 2 วินาที (20 รอบ รอบละ 100ms) ก่อนจะกลับไปตรวจเช็คพอร์ตใหม่
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;
                int simAdc = (int)((Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);
                _stateStore.Update(simAdc, "Simulation Mode (Sine Wave)");
                try { await Task.Delay(100, stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }
    }
}
