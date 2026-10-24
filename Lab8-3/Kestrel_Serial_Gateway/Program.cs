using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// 1. ลงทะเบียน StateStore เป็น Singleton (เข้าถึงข้อมูลจุดเดียวกันตลอดอายุแอป)
builder.Services.AddSingleton<TelemetryStateStore>();

// 2. ลงทะเบียน Background Worker ดักอ่านค่าจาก USB
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

app.MapGet("/", () => "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

// Endpoint สำหรับดึงค่า Telemetry พร้อม Logic ตรวจสอบระดับการแจ้งเตือน (Micro-Challenge)
app.MapGet("/api/telemetry", (TelemetryStateStore state) => {
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();

    // ภารกิจท้าทาย: คำนวณสถานะ alertLevel จาก percentage
    string alert = percent switch
    {
        > 85.0 => "DANGER (HIGH)",
        >= 70.0 => "WARNING",
        _ => "NORMAL"
    };

    return Results.Ok(new {
        sensor = "ESP32-Potentiometer",
        rawValue = raw,
        voltage = voltage,
        percentage = percent,
        alertLevel = alert,
        dataSource = source,
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    });
});

app.Run();

// ============================================================================
// 1. THREAD-SAFE STATE STORE
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
// 2. BACKGROUND WORKER พร้อมโหมดจำลองอัตโนมัติ
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;

    public SerialBridgeWorker(TelemetryStateStore stateStore, ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();

            // ระบุพอร์ตของ ESP32 หรือตั้ง null เพื่อเลือกอัตโนมัติ
            string? targetPort = "COM5";

            // ถ้าระบุพอร์ตไว้ ต้องเจอพอร์ตนั้นจริงเท่านั้น ไม่งั้นเข้าโหมดจำลอง (กันไปเกาะพอร์ต Bluetooth)
            bool portReady = string.IsNullOrEmpty(targetPort)
                ? availablePorts.Length > 0
                : availablePorts.Contains(targetPort, StringComparer.OrdinalIgnoreCase);

            if (portReady)
            {
                _logger.LogInformation("📋 รายการ COM Ports ในระบบ: [{Ports}]", string.Join(", ", availablePorts));

                string selectedPort = !string.IsNullOrEmpty(targetPort)
                    ? targetPort
                    : (availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase)) ?? availablePorts[0]);

                _logger.LogInformation("🔌 กำลังทดลองเชื่อมต่อพอร์ต: {Port}", selectedPort);

                try
                {
                    using var serial = new SerialPort(selectedPort, 115200) { ReadTimeout = 2000 };
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
                                if (int.TryParse(line, out int val))
                                {
                                    _stateStore.Update(val, $"Live Hardware ({selectedPort})");
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
                    break; // กด Ctrl+C ปิดโปรแกรม
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

            // เข้าโหมดจำลองสัญญาณ (Sine Wave) หากไม่มีสายต่อหรือเปิดพอร์ตไม่ได้
            try
            {
                for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
                {
                    double t = Environment.TickCount64 / 1000.0;
                    int simAdc = (int)((Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);
                    _stateStore.Update(simAdc, "Simulation Mode (Sine Wave)");
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}