using System.IO.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน State Store และ Background Worker
builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<DualSerialBridgeWorker>();

var app = builder.Build();

// เปิดใช้งานการเสิร์ฟไฟล์ HTML/CSS/JS จากโฟลเดอร์ wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoint สถานะระบบ (System Status)
app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway",
    studentId = "67030311",
    studentName = "Paphatsorn Aermsaarrd",
    leftGauge = "Liquid Level Tank (Gauge 4)",
    rightGauge = "Audio VU Meter (Gauge 2)",
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o")
}));

// Endpoint ส่งข้อมูล Telemetry 2 ช่องสัญญาณ (Channel A และ Channel B)
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// =============================================================================
// คลังข้อมูลส่วนกลาง 2 ช่องสัญญาณ (Thread-Safe State Store)
// =============================================================================
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _source = "Waiting for Hardware...";
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
                    name = "Potentiometer (Hardware / Channel A)",
                    rawValue = _rawA,
                    voltage = Math.Round((_rawA / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawA / 4095.0) * 100.0, 1)
                },
                channelB = new
                {
                    name = "Room Sensor / LDR (Simulated / Channel B)",
                    rawValue = _rawB,
                    voltage = Math.Round((_rawB / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawB / 4095.0) * 100.0, 1)
                },
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("o")
            };
        }
    }
}

// =============================================================================
// Background Worker ดักฟัง Serial Port จาก ESP32 จริง (Hardware Direct)
// =============================================================================
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
        _logger.LogInformation("🚀 DualSerialBridgeWorker เริ่มต้นการทำงาน...");

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();
            string? targetPort = availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase));

            if (targetPort != null)
            {
                SerialPort? serial = null;
                try
                {
                    serial = new SerialPort(targetPort, 115200)
                    {
                        ReadTimeout = 1000,
                        DtrEnable = false,
                        RtsEnable = false
                    };
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อฮาร์ดแวร์พอร์ต {Port} สำเร็จ (115200 bps)", targetPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            if (serial.BytesToRead > 0)
                            {
                                string line = serial.ReadLine().Trim();
                                if (!string.IsNullOrEmpty(line))
                                {
                                    if (line.Contains(','))
                                    {
                                        // กรณีส่งมาเป็นคู่จากเซนเซอร์ 2 ตัว: "rawA,rawB"
                                        var parts = line.Split(',');
                                        if (parts.Length >= 2 && int.TryParse(parts[0], out int vA) && int.TryParse(parts[1], out int vB))
                                        {
                                            _stateStore.Update(vA, vB, $"Live ({targetPort})");
                                        }
                                    }
                                    else if (int.TryParse(line, out int vA))
                                    {
                                        // ค่าจาก Potentiometer ฮาร์ดแวร์จริงบน ESP32 (Channel A)
                                        // จะเปลี่ยนก็ต่อเมื่อหมุน Potentiometer เท่านั้น ไม่มีการขยับขึ้นลงเอง
                                        double t = Environment.TickCount64 / 1000.0;
                                        int simB = (int)((Math.Sin(t * 1.2) + 1.0) / 2.0 * 4095);
                                        _stateStore.Update(vA, simB, $"Live ({targetPort}) + Sim B");
                                    }
                                }
                            }
                        }
                        catch (TimeoutException) { }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Serial Read Warning: {Msg}", ex.Message);
                        }

                        await Task.Delay(25, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ พอร์ต {Port} ({Message})", targetPort, ex.Message);
                }
                finally
                {
                    if (serial != null)
                    {
                        try { if (serial.IsOpen) serial.Close(); } catch { }
                        serial.Dispose();
                    }
                }
            }

            await Task.Delay(500, stoppingToken);
        }
    }
}
