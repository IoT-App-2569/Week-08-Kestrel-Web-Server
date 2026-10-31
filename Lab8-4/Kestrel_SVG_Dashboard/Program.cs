using System.IO.Ports;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// API: Telemetry (หน้าเว็บใน wwwroot จะเรียก endpoint นี้ด้วย fetch)
app.MapGet("/api/telemetry", (TelemetryStateStore store) =>
{
    var s = store.GetSnapshot();
    return Results.Ok(new
    {
        sensor = s.Sensor,
        rawValue = s.RawValue,
        voltage = s.Voltage,
        percentage = s.Percentage,
        alertLevel = s.AlertLevel,
        dataSource = s.DataSource,
        timestamp = s.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    });
});

// เปิดใช้งานการเสิร์ฟไฟล์สถิต (HTML, CSS, JS) ใน wwwroot และเปิด index.html อัตโนมัติ
app.UseFileServer();

app.Run();

// ================================================================
// Telemetry State Store (Thread-Safe)
// ================================================================
public record TelemetrySnapshot(
    string Sensor,
    int RawValue,
    double Voltage,
    double Percentage,
    string AlertLevel,
    string DataSource,
    DateTime Timestamp
);

public class TelemetryStateStore
{
    private readonly object _lock = new();

    private TelemetrySnapshot _current = new(
        "ESP32-Potentiometer", 0, 0.0, 0.0, "NORMAL", "Initializing", DateTime.UtcNow);

    // ค่า ADC สูงสุดที่บอร์ดอ่านได้จริงเมื่อหมุนสุด (ของคุณวัดได้ ~3215)
    // ต้องการสูตรตามใบงานเดิมให้เปลี่ยนเป็น 4095.0
    private const double MaxAdc = 3250.0;

    public void Update(int rawValue, string dataSource)
    {
        double ratio = Math.Clamp(rawValue / MaxAdc, 0.0, 1.0);
        double voltage = Math.Round(ratio * 3.3, 2);
        double percentage = Math.Round(ratio * 100.0, 1);

        string alertLevel =
            percentage > 85.0 ? "DANGER (HIGH)" :
            percentage >= 70.0 ? "WARNING" :
            "NORMAL";

        lock (_lock)
        {
            _current = new TelemetrySnapshot(
                "ESP32-Potentiometer", rawValue, voltage, percentage,
                alertLevel, dataSource, DateTime.UtcNow);
        }
    }

    public TelemetrySnapshot GetSnapshot()
    {
        lock (_lock) { return _current; }
    }
}

// ================================================================
// Serial Bridge Worker (Live Hardware + Simulation Fallback)
// ================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly ILogger<SerialBridgeWorker> _logger;
    private readonly TelemetryStateStore _stateStore;

    // รับได้ทั้ง "354", "ADC:354,12345", "ADC: 354", "raw=354"
    // (ไม่รับบรรทัด boot ของ ESP32 เช่น "configsip: 0, SPIWP:0xee")
    private static readonly Regex AdcPattern = new(
        @"^\s*(?:(?:ADC|RAW|VALUE)\s*[:=]\s*)?(\d{1,4})\s*(?:,.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SerialBridgeWorker(ILogger<SerialBridgeWorker> logger, TelemetryStateStore stateStore)
    {
        _logger = logger;
        _stateStore = stateStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        const string targetPort = "COM3";   // <-- เปลี่ยนเป็นพอร์ต ESP32 ของคุณ
        const int baudRate = 115200;
        const int noDataTimeoutMs = 5000;   // ไม่มีข้อมูลเกินนี้ -> ปิดแล้วเปิดพอร์ตใหม่

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();

                if (ports.Contains(targetPort, StringComparer.OrdinalIgnoreCase))
                {
                    using var serial = new SerialPort(targetPort, baudRate);
                    serial.NewLine = "\n";
                    serial.ReadTimeout = 500;
                    serial.DtrEnable = false;   // กัน DTR/RTS กดบอร์ดค้างในโหมดรีเซ็ต
                    serial.RtsEnable = false;
                    serial.Open();

                    // พัลส์รีเซ็ต ESP32 (RTS ต่อกับขา EN) เลียนแบบที่ idf.py monitor ทำตอนเปิดพอร์ต
                    serial.DtrEnable = false;
                    serial.RtsEnable = true;
                    await Task.Delay(100, stoppingToken);
                    serial.RtsEnable = false;

                    await Task.Delay(500, stoppingToken);   // รอให้บอร์ดบูต
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อ {Port} สำเร็จ (Live Mode) รอข้อมูลจากบอร์ด...", targetPort);

                    long lastValidTick = Environment.TickCount64;
                    long lastHeartbeat = Environment.TickCount64;
                    int rxLogged = 0;
                    int lastVal = 0;

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            string line = serial.ReadLine().Trim();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            rxLogged++;

                            var m = AdcPattern.Match(line);
                            if (m.Success && int.TryParse(m.Groups[1].Value, out int val))
                            {
                                val = Math.Clamp(val, 0, 4095);
                                _stateStore.Update(val, $"Live Hardware ({targetPort})");
                                lastValidTick = Environment.TickCount64;
                                lastVal = val;
                            }
                            else if (rxLogged <= 15)
                            {
                                _logger.LogInformation("RX (ข้าม): {Line}", line);
                            }

                            // สรุปทุก 1 วินาที: รับมากี่บรรทัด และค่าล่าสุดคือเท่าไหร่
                            if (Environment.TickCount64 - lastHeartbeat > 1000)
                            {
                                _logger.LogInformation("📊 รับแล้ว {N} บรรทัด | ค่าล่าสุด = {V}", rxLogged, lastVal);
                                lastHeartbeat = Environment.TickCount64;
                            }
                        }
                        catch (TimeoutException)
                        {
                            // ไม่มีบรรทัดใหม่ในรอบนี้ ตรวจว่าเงียบนานเกินไปหรือไม่
                        }

                        if (Environment.TickCount64 - lastValidTick > noDataTimeoutMs)
                        {
                            _logger.LogWarning("⏱️ ไม่ได้รับตัวเลขจากบอร์ดเกิน {Sec} วินาที -> ปิดแล้วเปิดพอร์ตใหม่",
                                noDataTimeoutMs / 1000);
                            break;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("🔍 ไม่พบ {Port} (พบ: [{Ports}]) -> Simulation Mode",
                        targetPort, string.Join(", ", ports));
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                // เช่น ถอดสาย USB หรือพอร์ตถูกโปรแกรมอื่นจับอยู่
                _logger.LogWarning("⚠️ พอร์ตมีปัญหา ({Msg}) -> Simulation Mode", ex.Message);
            }

            // Fallback: จำลองคลื่น Sine 2 วินาที แล้ววนกลับไปลองต่อพอร์ตใหม่
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;
                int simAdc = (int)((Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);
                _stateStore.Update(simAdc, "Simulation Mode (Sine Wave)");

                try { await Task.Delay(100, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}