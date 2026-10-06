// ============================================================================
// ใบงานที่ 8.5: Dual-Channel IoT Command Center (Kestrel_Dual_Dashboard)
// นักศึกษา: 67030011 กฤตนัย บุญน้อย
// เกจ์ซ้าย (Channel A) = 4 Liquid Level Tank, เกจ์ขวา (Channel B) = 3 Retro 7-Segment
// ============================================================================
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<DualSerialBridgeWorker>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoint สถานะระบบ
app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway",
    studentId = "67030011",
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
}));

// Endpoint ส่งข้อมูล Telemetry 2 ช่อง
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// ============================================================================
// คลังข้อมูลส่วนกลาง 2 ช่องสัญญาณ (Thread-Safe)
// ============================================================================
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _nameA = "Potentiometer (Hardware)";
    private string _nameB = "Room Sensor (Simulated)";
    private string _source = "Initializing";
    private DateTime _lastUpdated = DateTime.UtcNow;

    public void Update(int rawA, int rawB, string source, bool channelAIsHardware = true, bool channelBIsHardware = false)
    {
        lock (_lock)
        {
            _rawA = Math.Clamp(rawA, 0, 4095);
            _rawB = Math.Clamp(rawB, 0, 4095);
            _nameA = channelAIsHardware ? "Potentiometer (Hardware)" : "Potentiometer (Simulated)";
            _nameB = channelBIsHardware ? "LDR / Sensor B (Hardware GPIO 35)" : "Room Sensor (Simulated)";
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    public object GetSnapshot()
    {
        lock (_lock)
        {
            return new
            {
                channelA = Channel(_nameA, _rawA),
                channelB = Channel(_nameB, _rawB),
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
            };
        }
    }

    private static object Channel(string name, int raw) => new
    {
        name,
        rawValue = raw,
        voltage = Math.Round((raw / 4095.0) * 3.3, 2),
        percentage = Math.Round((raw / 4095.0) * 100.0, 1)
    };
}

// ============================================================================
// การเลือกพอร์ต Serial ให้ทำงานได้ทั้ง Windows และ macOS / Linux
// ============================================================================
public static class SerialPortPicker
{
    private static readonly string[] UsbHints = { "usbserial", "usbmodem", "SLAB_USBtoUART", "wchusbserial", "ttyUSB", "ttyACM" };

    public static string? Pick(string[] availablePorts, string? targetPort)
    {
        // 1) ถ้ากำหนดพอร์ตไว้ และมีอยู่จริงในระบบ ใช้พอร์ตนั้นเลย
        //    (บน macOS/Linux เช็คไฟล์อุปกรณ์ด้วย เพราะ GetPortNames() ไม่แสดง /dev/ttys* หรือ pty)
        if (!string.IsNullOrEmpty(targetPort) &&
            (availablePorts.Contains(targetPort, StringComparer.OrdinalIgnoreCase) || (!OperatingSystem.IsWindows() && File.Exists(targetPort))))
            return targetPort;

        if (OperatingSystem.IsWindows())
            return availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase));

        // macOS: ตัดพอร์ต Bluetooth ทิ้ง และใช้ /dev/cu.* เท่านั้น (/dev/tty.* จะบล็อกรอ DCD ตอน Open)
        return availablePorts
            .Where(p => UsbHints.Any(h => p.Contains(h, StringComparison.OrdinalIgnoreCase)))
            .Where(p => !OperatingSystem.IsMacOS() || p.StartsWith("/dev/cu.", StringComparison.Ordinal))
            .FirstOrDefault();
    }
}

// ============================================================================
// Background Worker ดักฟัง Serial และสร้างสัญญาณจำลอง
// ============================================================================
public class DualSerialBridgeWorker : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;
    private readonly ILogger<DualSerialBridgeWorker> _logger;
    private readonly string? _targetPort;
    private static readonly TimeSpan DataTimeout = TimeSpan.FromSeconds(3);

    public DualSerialBridgeWorker(DualChannelStateStore stateStore, ILogger<DualSerialBridgeWorker> logger, IConfiguration config)
    {
        _stateStore = stateStore;
        _logger = logger;
        _targetPort = config["Serial:Port"];
    }

    // Channel B จำลองแบบ Sine Wave ช้าๆ (คาบประมาณ 5.2 วินาที)
    private static int SimulateB() =>
        (int)((Math.Sin(Environment.TickCount64 / 1000.0 * 1.2) + 1.0) / 2.0 * 4095);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        bool loggedSimulation = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            string? targetPort = SerialPortPicker.Pick(SerialPort.GetPortNames(), _targetPort);
            if (targetPort is not null)
            {
                try
                {
                    using var serial = new SerialPort(targetPort, 115200) { ReadTimeout = 2000 };
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์พอร์ต {Port} สำเร็จ", targetPort);
                    loggedSimulation = false;

                    // Watchdog: ข้อมูลเงียบเกิน DataTimeout = ถือว่าสายหลุด (บาง OS ไม่โยน Exception ตอนถอดสาย)
                    var lastDataAt = DateTime.UtcNow;

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        if (DateTime.UtcNow - lastDataAt > DataTimeout)
                        {
                            _logger.LogWarning("⚠️ ไม่มีข้อมูลจาก {Port} เกิน {Sec} วินาที -> สลับเข้าโหมดจำลอง", targetPort, DataTimeout.TotalSeconds);
                            break;
                        }

                        // อ่านทุกบรรทัดที่ค้างอยู่ในบัฟเฟอร์ก่อนพัก เพื่อไม่ให้ข้อมูลล้าหลังมือที่หมุน
                        while (serial.BytesToRead > 0)
                        {
                            string line = serial.ReadLine().Trim();
                            var parts = line.Split(',');
                            if (parts.Length >= 2 && int.TryParse(parts[0], out int vA) && int.TryParse(parts[1], out int vB))
                            {
                                _stateStore.Update(vA, vB, $"Live ({targetPort})", channelBIsHardware: true);
                                lastDataAt = DateTime.UtcNow;
                            }
                            else if (int.TryParse(line, out int a))
                            {
                                _stateStore.Update(a, SimulateB(), $"Live ({targetPort}) + Sim B");
                                lastDataAt = DateTime.UtcNow;
                            }
                            // บรรทัดอื่น (เช่น Boot Log ของ ESP32) ข้ามไป
                        }
                        await Task.Delay(20, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // ไม่กลืน Exception เงียบๆ (catch { }) เพื่อให้รู้ว่าทำไมหลุดเข้าโหมดจำลอง
                    _logger.LogWarning("⚠️ พอร์ต {Port} ใช้งานไม่ได้ ({Msg}) -> สลับเข้าโหมดจำลอง", targetPort, ex.Message);
                }
            }
            else if (!loggedSimulation)
            {
                _logger.LogInformation("🔍 ไม่พบพอร์ต USB -> ทำงานในโหมดจำลอง 2 ช่องสัญญาณ");
                loggedSimulation = true;
            }

            // Fallback เมื่อไม่ได้เสียบสาย USB
            double time = Environment.TickCount64 / 1000.0;
            int sA = (int)((Math.Sin(time * 0.8) + 1.0) / 2.0 * 4095);
            int sB = (int)((Math.Cos(time * 1.4) + 1.0) / 2.0 * 4095);
            _stateStore.Update(sA, sB, "Simulation Mode (2 Channels)", channelAIsHardware: false);
            try { await Task.Delay(100, stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
