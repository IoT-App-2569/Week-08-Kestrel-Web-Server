using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียนคลังข้อมูล 2 ช่องสัญญาณ (Thread-Safe Singleton)
builder.Services.AddSingleton<DualChannelStateStore>();

// ลงทะเบียน Worker เชื่อมต่อ Serial และสร้างสัญญาณจำลองช่อง B
builder.Services.AddHostedService<DualSerialBridgeWorker>();

var app = builder.Build();

// เปิดใช้งานการเสิร์ฟไฟล์สถิตสำหรับแดชบอร์ด (wwwroot/index.html)
app.UseFileServer();

// Endpoint สถานะระบบ
app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway (67030073)",
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o")
}));

// Endpoint ส่งข้อมูล Telemetry 2 ช่องสัญญาณ
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// ============================================================================
// 1. THREAD-SAFE DUAL CHANNEL STATE STORE
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
            double voltA = Math.Round((_rawA / 4095.0) * 3.3, 2);
            double pctA = Math.Round((_rawA / 4095.0) * 100.0, 1);

            double voltB = Math.Round((_rawB / 4095.0) * 3.3, 2);
            double pctB = Math.Round((_rawB / 4095.0) * 100.0, 1);

            return new
            {
                studentId = "67030073",
                channelA = new
                {
                    name = "Liquid Level Tank (Hardware Potentiometer)",
                    gaugeType = "Liquid Level Tank",
                    rawValue = _rawA,
                    voltage = voltA,
                    percentage = pctA
                },
                channelB = new
                {
                    name = "Audio VU Meter (Hardware LDR Sensor)",
                    gaugeType = "Audio VU Meter",
                    rawValue = _rawB,
                    voltage = voltB,
                    percentage = pctB
                },
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
    }
}

// ============================================================================
// 2. DUAL-CHANNEL BACKGROUND WORKER
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();
            string? targetPort = availablePorts.FirstOrDefault(p => p.Contains("cu.usbserial", StringComparison.OrdinalIgnoreCase))
                ?? availablePorts.FirstOrDefault(p => p.Contains("tty.usbserial", StringComparison.OrdinalIgnoreCase))
                ?? availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase))
                ?? availablePorts.FirstOrDefault();

            if (!string.IsNullOrEmpty(targetPort))
            {
                _logger.LogInformation("📋 ตรวจพบ Serial Port: {Port}", targetPort);

                try
                {
                    using var serial = new SerialPort(targetPort, 115200)
                    {
                        ReadTimeout = 2000,
                        DtrEnable = true,
                        RtsEnable = true
                    };
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์ CH-A บน {Port} (Live Mode)", targetPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        if (serial.BytesToRead > 0)
                        {
                            string line = serial.ReadLine().Trim();

                            // รองรับทั้งแบบส่งค่าคู่ (valA,valB) และแบบส่งค่าเดี่ยว (valA + simB)
                            if (line.Contains(','))
                            {
                                var parts = line.Split(',');
                                if (parts.Length >= 2 && int.TryParse(parts[0], out int vA) && int.TryParse(parts[1], out int vB))
                                {
                                    _stateStore.Update(vA, vB, $"Live Hardware ({targetPort})");
                                }
                            }
                            else if (int.TryParse(line, out int vA))
                            {
                                // Channel B: สัญญาณจำลองคลื่นเสียง/ระดับเสียงแกว่งอย่างนุ่มนวล
                                double t = Environment.TickCount64 / 1000.0;
                                int simB = (int)((Math.Sin(t * 2.5) * 0.5 + 0.5) * 4095);
                                _stateStore.Update(vA, simB, $"Live ({targetPort}) + Simulated VU");
                            }
                        }
                        await Task.Delay(40, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ ไม่สามารถเปิด {Port}: {Msg} -> สลับโหมดจำลอง 2 ช่อง", targetPort, ex.Message);
                }
            }

            // Fallback เมื่อไม่ได้เสียบสาย USB (จำลองทั้ง 2 ช่องสัญญาณ)
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double time = Environment.TickCount64 / 1000.0;
                int sA = (int)((Math.Sin(time * 0.8) + 1.0) / 2.0 * 4095);
                int sB = (int)((Math.Cos(time * 2.2) * 0.5 + 0.5) * 4095);
                _stateStore.Update(sA, sB, "Simulation Mode (Dual Channel)");
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
