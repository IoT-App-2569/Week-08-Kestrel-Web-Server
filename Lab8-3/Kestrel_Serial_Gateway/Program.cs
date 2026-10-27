
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน State Store และ Background Worker
builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// หน้าแรก
app.MapGet("/", () =>
    "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

// API สำหรับอ่านค่าเซนเซอร์
app.MapGet("/api/telemetry", (TelemetryStateStore state) =>
{
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();

    // Micro-Challenge: คำนวณระดับการแจ้งเตือน
    string alertLevel;

    if (percent > 85.0)
    {
        alertLevel = "DANGER (HIGH)";
    }
    else if (percent >= 70.0)
    {
        alertLevel = "WARNING";
    }
    else
    {
        alertLevel = "NORMAL";
    }

    return Results.Ok(new
    {
        sensor = "ESP32-Potentiometer",
        rawValue = raw,
        voltage = voltage,
        percentage = percent,
        alertLevel = alertLevel,
        dataSource = source,
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    });
});

app.Run();

// ============================================================
// 1. THREAD-SAFE STATE STORE
// ============================================================
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
            _rawValue = Math.Clamp(rawValue, 0, 4095);
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    public (int raw, double voltage, double percent,
        string source, DateTime updated) GetSnapshot()
    {
        lock (_lock)
        {
            double voltage = Math.Round((_rawValue / 4095.0) * 3.3, 2);
            double percent = Math.Round((_rawValue / 4095.0) * 100.0, 1);

            return (_rawValue, voltage, percent, _source, _lastUpdated);
        }
    }
}

// ============================================================
// 2. BACKGROUND WORKER: SERIAL BRIDGE + SIMULATION
// ============================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;

    public SerialBridgeWorker(
        TelemetryStateStore stateStore,
        ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] ports = SerialPort.GetPortNames();

            // รองรับพอร์ต ESP32 บน Mac และ Windows
            string? selectedPort = ports.FirstOrDefault(p =>
                p.Equals("/dev/cu.usbserial-0001",
                    StringComparison.OrdinalIgnoreCase));

            selectedPort ??= ports.FirstOrDefault(p =>
                p.Contains("usbserial", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("SLAB_USBtoUART", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("wchusbserial", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("usbmodem", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("COM", StringComparison.OrdinalIgnoreCase));

            if (selectedPort != null)
            {
                try
                {
                    _logger.LogInformation(
                        "กำลังเชื่อมต่อพอร์ต {Port}", selectedPort);

                    using var serial = new SerialPort(selectedPort, 115200)
                    {
                        ReadTimeout = 2000
                    };

                    serial.Open();
                    serial.DiscardInBuffer();

                    _logger.LogInformation(
                        "เชื่อมต่อฮาร์ดแวร์สำเร็จบน {Port} (Live Mode)",
                        selectedPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            string line = serial.ReadLine().Trim();

                            if (int.TryParse(line, out int value)
                                && value >= 0 && value <= 4095)
                            {
                                _stateStore.Update(
                                    value, $"Live Hardware ({selectedPort})");
                            }
                        }
                        catch (TimeoutException)
                        {
                            // ไม่มีข้อมูลใหม่ ให้รออ่านต่อ
                        }
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "เปิดหรืออ่านพอร์ต {Port} ไม่สำเร็จ: {Message}",
                        selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation(
                    "ไม่พบพอร์ต Serial กำลังใช้ Simulation Mode");
            }

            // จำลองค่า ADC ในช่วง 0-4095
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;
                int simAdc = (int)(
                    (Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);

                _stateStore.Update(
                    simAdc, "Simulation Mode (Sine Wave)");

                try
                {
                    await Task.Delay(100, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
