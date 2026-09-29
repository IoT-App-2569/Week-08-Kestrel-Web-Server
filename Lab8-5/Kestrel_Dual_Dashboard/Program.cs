using System.IO.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o")
}));

// Endpoint ส่งข้อมูล Telemetry 2 ช่อง
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// คลังข้อมูลส่วนกลาง 2 ช่องสัญญาณ (Thread-Safe)
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _source = "Waiting for Dual Hardware (COM5)";
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
                    name = "Sensor A (Hardware)",
                    rawValue = _rawA,
                    voltage = Math.Round((_rawA / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawA / 4095.0) * 100.0, 1)
                },
                channelB = new
                {
                    name = "Sensor B (Simulated / LDR)",
                    rawValue = _rawB,
                    voltage = Math.Round((_rawB / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawB / 4095.0) * 100.0, 1)
                },
                studentId = "67030109",
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("o")
            };
        }
    }
}

// Background Worker ดักฟัง Serial และสร้างสัญญาณจำลอง
public class DualSerialBridgeWorker : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;
    private readonly ILogger<DualSerialBridgeWorker> _logger;
    private readonly IConfiguration _configuration;

    public DualSerialBridgeWorker(
        DualChannelStateStore stateStore,
        ILogger<DualSerialBridgeWorker> logger,
        IConfiguration configuration)
    {
        _stateStore = stateStore;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            string configuredPort = _configuration["SerialPort"] ?? string.Empty;
            string[] availablePorts = SerialPort.GetPortNames();
            string targetPort = string.IsNullOrWhiteSpace(configuredPort)
                ? availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase)) ?? string.Empty
                : configuredPort;

            if (!string.IsNullOrWhiteSpace(targetPort))
            {
                try
                {
                    using var serial = new SerialPort(targetPort, 115200) { ReadTimeout = 2000, DtrEnable = true, RtsEnable = true };
                    serial.NewLine = "\n";
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("เชื่อมต่อฮาร์ดแวร์พอร์ต {Port} สำเร็จ (Live Mode)", targetPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        if (serial.BytesToRead > 0)
                        {
                            string line = serial.ReadLine().Trim();
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            if (line.Contains(','))
                            {
                                var parts = line.Split(',');
                                if (parts.Length >= 2 && int.TryParse(parts[0], out int vA))
                                {
                                    double simulationTime = Environment.TickCount64 / 1000.0;
                                    int simulatedB = (int)((Math.Sin(simulationTime * 1.2) + 1.0) / 2.0 * 4095);
                                    _stateStore.Update(vA, simulatedB, $"Live ({targetPort}) + Sim B");
                                }
                                else
                                    _logger.LogDebug("ข้ามข้อมูล Dual ADC ที่ไม่ถูกต้อง: {Line}", line);
                            }
                            else if (int.TryParse(line, out int singleValue))
                            {
                                double simulationTime = Environment.TickCount64 / 1000.0;
                                int simulatedB = (int)((Math.Sin(simulationTime * 1.2) + 1.0) / 2.0 * 4095);
                                _stateStore.Update(singleValue, simulatedB, $"Live ({targetPort}) + Sim B");
                            }
                            // ESP32 prints reset and boot messages before ADC data.
                        }
                        await Task.Delay(50, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("ไม่สามารถเปิดพอร์ต {Port}: {Message} กำลังรอข้อมูลจริงจาก ESP32", targetPort, ex.Message);
                }
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            _logger.LogWarning("ยังไม่มีข้อมูล Dual ADC จาก {Port}; รอข้อมูลจริงจาก ESP32", targetPort);
            await Task.Delay(100, stoppingToken);
        }
    }
}