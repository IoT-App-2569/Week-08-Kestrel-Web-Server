using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<DualSerialBridgeWorker>();

var app = builder.Build();

// เสิร์ฟ wwwroot/index.html เป็นหน้าแรก
app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoint สถานะระบบ
app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway",
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o")
}));

// Endpoint ส่งข้อมูล Telemetry 2 ช่อง
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// ============================================================================
// 1. คลังข้อมูลส่วนกลาง 2 ช่องสัญญาณ (Thread-Safe)
// ============================================================================
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _source = "Initializing";
    private DateTime _lastUpdated = DateTime.UtcNow;

    public void Update(int rawA, int rawB, string source)
    {
        lock (_lock)
        {
            _rawA = Math.Clamp(rawA, 0, 4095);
            _rawB = Math.Clamp(rawB, 0, 4095);
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
                channelA = new
                {
                    name = "Potentiometer (Hardware)",
                    rawValue = _rawA,
                    voltage = Math.Round((_rawA / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawA / 4095.0) * 100.0, 1)
                },
                channelB = new
                {
                    name = "Room Sensor (Simulated)",
                    rawValue = _rawB,
                    voltage = Math.Round((_rawB / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawB / 4095.0) * 100.0, 1)
                },
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
    }
}

// ============================================================================
// 2. Background Worker ดักฟัง Serial + สร้างสัญญาณจำลองช่อง B
// ============================================================================
public class DualSerialBridgeWorker : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;
    private readonly ILogger<DualSerialBridgeWorker> _logger;

    public DualSerialBridgeWorker(DualChannelStateStore stateStore, ILogger<DualSerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    // สัญญาณจำลองช่อง B (Sine Wave)
    private static int SimB()
    {
        double t = Environment.TickCount64 / 1000.0;
        return (int)((Math.Sin(t * 1.2) + 1.0) / 2.0 * 4095);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();

            // 👉 พอร์ตของ ESP32 (COM6, COM7 บนเครื่องนี้เป็น Bluetooth ห้ามใช้)
            string? targetPort = "COM5";

            // ถ้าระบุพอร์ตไว้ ต้องเจอพอร์ตนั้นจริงเท่านั้น ไม่งั้นเข้าโหมดจำลอง
            bool portReady = string.IsNullOrEmpty(targetPort)
                ? availablePorts.Length > 0
                : availablePorts.Contains(targetPort, StringComparer.OrdinalIgnoreCase);

            if (portReady)
            {
                string selectedPort = !string.IsNullOrEmpty(targetPort)
                    ? targetPort
                    : (availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase)) ?? availablePorts[0]);

                try
                {
                    using var serial = new SerialPort(selectedPort, 115200) { ReadTimeout = 2000 };
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์พอร์ต {Port} สำเร็จ", selectedPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            if (serial.BytesToRead > 0)
                            {
                                string line = serial.ReadLine().Trim();

                                if (line.Contains(','))
                                {
                                    // Bonus: ESP32 ส่งมา 2 ค่า เช่น "2048,1500"
                                    var parts = line.Split(',');
                                    if (parts.Length >= 2 && int.TryParse(parts[0], out int vA) && int.TryParse(parts[1], out int vB))
                                        _stateStore.Update(vA, vB, $"Live ESP32 ({selectedPort})");
                                }
                                else if (int.TryParse(line, out int vA))
                                {
                                    // ปกติ: ช่อง A จากบอร์ดจริง + ช่อง B จำลอง
                                    _stateStore.Update(vA, SimB(), $"Live ESP32 ({selectedPort}) + Sim B");
                                }
                            }
                            else
                            {
                                await Task.Delay(20, stoppingToken);
                            }
                        }
                        catch (TimeoutException)
                        {
                            await Task.Delay(20, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ ไม่สามารถใช้งาน {Port} ({Msg}) -> สลับเข้าโหมดจำลอง", selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("🔍 ไม่พบพอร์ต {Port} -> ทำงานในโหมดจำลอง 2 ช่อง", targetPort);
            }

            // Fallback: จำลองทั้ง 2 ช่อง 2 วินาที แล้วกลับไปเช็คพอร์ตใหม่
            try
            {
                for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
                {
                    double time = Environment.TickCount64 / 1000.0;
                    int sA = (int)((Math.Sin(time * 0.8) + 1.0) / 2.0 * 4095);
                    int sB = (int)((Math.Cos(time * 1.4) + 1.0) / 2.0 * 4095);
                    _stateStore.Update(sA, sB, "Simulation Mode (2 Channels)");
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