using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

app.MapGet("/", () => "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

app.MapGet("/api/telemetry", (TelemetryStateStore state) =>
{
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();

    string alertLevel = percent > 85.0 ? "DANGER (HIGH)"
                      : percent >= 70.0 ? "WARNING"
                      : "NORMAL";

    return Results.Ok(new
    {
        sensor = "ESP32-Potentiometer",
        rawValue = raw,
        voltage,
        percentage = percent,
        alertLevel,
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
            _rawValue = Math.Clamp(rawValue, 0, 4095);
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    public (int raw, double voltage, double percent, string source, DateTime updated) GetSnapshot()
    {
        lock (_lock)
        {
            double voltage = Math.Round(_rawValue / 4095.0 * 3.3, 2);
            double percent = Math.Round(_rawValue / 4095.0 * 100.0, 1);
            return (_rawValue, voltage, percent, _source, _lastUpdated);
        }
    }
}

// ============================================================================
// 2. BACKGROUND WORKER
//    - Simulation loop: รันแยกตลอดเวลา เขียนค่าเมื่อไม่มีข้อมูลจริงใน 2 วินาที
//    - Live loop: ตรวจจับพอร์ตอัตโนมัติและอ่านค่าจากบอร์ด
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    // ---------- ตั้งค่าตรงนี้ (ปกติไม่ต้องแก้) ----------
    private const string ManualPort = "";                         // ใส่เช่น "COM5" เพื่อบังคับพอร์ต
    private const int BaudRate = 115200;
    private static readonly bool ForceSimulation = false;         // true = จำลองอย่างเดียว ไม่แตะพอร์ต
    private static readonly TimeSpan NoDataTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LiveFreshness = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private static readonly string[] BoardKeywords =
    {
        "CP210", "CH340", "CH341", "CH910", "Silicon Labs",
        "USB-SERIAL", "USB Serial", "UART", "ESP32", "FTDI"
    };
    // ---------------------------------------------------

    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;
    private string _lastStatus = "";

    public SerialBridgeWorker(TelemetryStateStore stateStore, ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        // เริ่มโหมดจำลองทันที แยกเธรดจากส่วนต่อฮาร์ดแวร์
        var simulationTask = Task.Run(() => SimulationLoopAsync(stoppingToken), stoppingToken);

        if (ForceSimulation)
        {
            Status(LogLevel.Information, "🧪 ForceSimulation = true -> ใช้โหมดจำลองตลอดเวลา");
            await simulationTask;
            return;
        }

        // วนลองต่อฮาร์ดแวร์ซ้ำเรื่อยๆ (ค่าจริงจะมาแทนค่าจำลองเองเมื่อมีข้อมูลเข้า)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Run(() => RunLiveModeAsync(stoppingToken), stoppingToken);

            try { await Task.Delay(RetryDelay, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void Status(LogLevel level, string message)
    {
        if (message == _lastStatus) return;
        _lastStatus = message;
        _logger.Log(level, "{Message}", message);
    }

    // ---------------- SIMULATION LOOP ----------------
    private async Task SimulationLoopAsync(CancellationToken ct)
    {
        var rng = new Random();
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var snap = _stateStore.GetSnapshot();
                bool liveIsFresh = snap.source.StartsWith("Live Hardware")
                                   && DateTime.UtcNow - snap.updated < LiveFreshness;

                if (!liveIsFresh)
                {
                    double t = Environment.TickCount64 / 1000.0;
                    double wave = Math.Sin(t * 0.8) * 0.8 + Math.Sin(t * 3.1) * 0.2;
                    int adc = (int)((wave + 1.0) / 2.0 * 4095) + rng.Next(-15, 16);
                    _stateStore.Update(adc, "Simulation Mode (Sine Wave)");
                }

                await Task.Delay(100, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    // ---------------- AUTO-DETECT PORT ----------------
    private (string Port, string Description)? DetectBoardPort(string[] ports)
    {
        if (!string.IsNullOrEmpty(ManualPort))
        {
            return ports.Contains(ManualPort, StringComparer.OrdinalIgnoreCase)
                ? (ManualPort, "ระบุเอง")
                : null;
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'");

                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    var match = Regex.Match(name, @"\((COM\d+)\)");
                    if (!match.Success) continue;

                    string port = match.Groups[1].Value;
                    bool isBoard = BoardKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));

                    if (isBoard && ports.Contains(port, StringComparer.OrdinalIgnoreCase))
                        return (port, name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("ตรวจรายชื่ออุปกรณ์ไม่สำเร็จ: {Msg}", ex.Message);
            }
        }

        // ไม่เจอชิปที่รู้จัก -> ไม่เดาสุ่ม (กันไปเปิดพอร์ต Bluetooth แล้วค้าง)
        return null;
    }

    // ---------------- LIVE MODE ----------------
    private async Task RunLiveModeAsync(CancellationToken ct)
    {
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            Status(LogLevel.Information, "🔍 ไม่พบพอร์ต USB -> ใช้โหมดจำลอง (Simulation Mode)");
            return;
        }

        var detected = DetectBoardPort(ports);
        if (detected is null)
        {
            Status(LogLevel.Information, $"🔍 ไม่พบบอร์ด ESP32 ในรายการ [{string.Join(", ", ports)}] -> ใช้โหมดจำลอง");
            return;
        }

        var (port, description) = detected.Value;
        Status(LogLevel.Information, $"🔌 พบบอร์ดที่ {port} : {description}");

        try
        {
            using var serial = new SerialPort(port, BaudRate) { ReadTimeout = 2000 };
            serial.Open();
            serial.DiscardInBuffer();

            DateTime lastData = DateTime.UtcNow;
            bool gotData = false;

            while (!ct.IsCancellationRequested && serial.IsOpen)
            {
                try
                {
                    if (serial.BytesToRead > 0)
                    {
                        string line = serial.ReadLine().Trim();
                        if (int.TryParse(line, out int val))
                        {
                            _stateStore.Update(val, $"Live Hardware ({port})");
                            lastData = DateTime.UtcNow;

                            if (!gotData)
                            {
                                gotData = true;
                                Status(LogLevel.Information, $"✅ ได้รับข้อมูลจริงจาก {port} (Live Mode)");
                            }
                        }
                    }
                    else
                    {
                        if (DateTime.UtcNow - lastData > NoDataTimeout)
                        {
                            Status(LogLevel.Warning, $"⏱️ เปิด {port} ได้ แต่บอร์ดไม่ส่งข้อมูล -> ใช้โหมดจำลอง");
                            return;
                        }
                        await Task.Delay(50, ct);
                    }
                }
                catch (TimeoutException)
                {
                    await Task.Delay(50, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Status(LogLevel.Warning, $"⚠️ เปิด {port} ไม่ได้ ({ex.Message}) -> ใช้โหมดจำลอง");
        }
    }
}