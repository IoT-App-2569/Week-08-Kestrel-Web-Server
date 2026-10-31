using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// Services
// ================================================================
builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// ================================================================
// Live Dashboard Page
// ================================================================
app.MapGet("/", () =>
{
    return Results.Content(
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>ESP32 Telemetry Dashboard</title>
            <style>
                body { font-family: Arial, sans-serif; background: #f4f6f8; margin: 0; padding: 40px; }
                .container { max-width: 700px; margin: auto; }
                h1 { text-align: center; margin-bottom: 30px; }
                .card { background: white; border-radius: 12px; padding: 25px; margin-bottom: 15px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
                .label { color: #666; font-size: 14px; margin-bottom: 8px; }
                .value { font-size: 32px; font-weight: bold; }
                .source { font-size: 16px; color: #333; word-break: break-all; }
                .status { text-align: center; margin-top: 20px; color: #555; }
                .NORMAL { color: #1a9a4a; }
                .WARNING { color: #e08a00; }
                .DANGER { color: #d62828; }
            </style>
        </head>
        <body>
            <div class="container">
                <h1>ESP32 Telemetry Dashboard</h1>

                <div class="card">
                    <div class="label">Raw ADC Value</div>
                    <div class="value" id="rawValue">-</div>
                </div>

                <div class="card">
                    <div class="label">Voltage</div>
                    <div class="value"><span id="voltage">-</span> V</div>
                </div>

                <div class="card">
                    <div class="label">Percentage</div>
                    <div class="value"><span id="percentage">-</span> %</div>
                </div>

                <div class="card">
                    <div class="label">Alert Level</div>
                    <div class="value" id="alertLevel">-</div>
                </div>

                <div class="card">
                    <div class="label">Data Source</div>
                    <div class="source" id="source">-</div>
                </div>

                <div class="status">Last update: <span id="lastUpdate">-</span></div>
            </div>

            <script>
                async function updateTelemetry() {
                    try {
                        const response = await fetch('/api/telemetry');
                        if (!response.ok) throw new Error('HTTP error: ' + response.status);
                        const data = await response.json();

                        document.getElementById('rawValue').textContent = data.rawValue;
                        document.getElementById('voltage').textContent = Number(data.voltage).toFixed(2);
                        document.getElementById('percentage').textContent = Number(data.percentage).toFixed(1);

                        const alertEl = document.getElementById('alertLevel');
                        alertEl.textContent = data.alertLevel;
                        alertEl.className = 'value ' + data.alertLevel.split(' ')[0];

                        document.getElementById('source').textContent = data.dataSource;
                        document.getElementById('lastUpdate').textContent = new Date().toLocaleTimeString();
                    } catch (error) {
                        console.error('ไม่สามารถอ่านข้อมูลได้:', error);
                    }
                }

                updateTelemetry();
                setInterval(updateTelemetry, 500);
            </script>
        </body>
        </html>
        """,
        "text/html; charset=utf-8"
    );
});

// ================================================================
// API: Telemetry  (ข้อกำหนดหลักของใบงาน)
// ================================================================
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

    // ค่า ADC สูงสุดที่บอร์ดของคุณอ่านได้จริงเมื่อหมุนสุด (วัดได้ ~3215)
    // ถ้าต้องการใช้สูตรตามใบงานเดิม ให้เปลี่ยนเป็น 4095.0
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
                "ESP32-Potentiometer",
                rawValue,
                voltage,
                percentage,
                alertLevel,
                dataSource,
                DateTime.UtcNow);
        }
    }

    public TelemetrySnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return _current;
        }
    }
}

// ================================================================
// Serial Bridge Worker (Live Hardware + Simulation Fallback)
// ================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly ILogger<SerialBridgeWorker> _logger;
    private readonly TelemetryStateStore _stateStore;

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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();

                if (ports.Contains(targetPort, StringComparer.OrdinalIgnoreCase))
                {
                    using var serial = new SerialPort(targetPort, baudRate);
                    serial.NewLine = "\n";
                    serial.ReadTimeout = 1000;
                    serial.Open();
                    serial.DiscardInBuffer();
                    _logger.LogInformation("✅ เชื่อมต่อ {Port} สำเร็จ (Live Mode)", targetPort);

                    while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                    {
                        try
                        {
                            string line = serial.ReadLine().Trim();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // รองรับทั้ง "354" และ "ADC:354,12345"
                            string payload = line.StartsWith("ADC:")
                                ? line.Substring(4).Split(',')[0]
                                : line;

                            if (int.TryParse(payload, out int val))
                                _stateStore.Update(val, $"Live Hardware ({targetPort})");
                        }
                        catch (TimeoutException)
                        {
                            // ไม่มีข้อมูลในช่วงนี้ วนอ่านใหม่
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("🔍 ไม่พบ {Port} (พบ: [{Ports}]) -> Simulation Mode",
                        targetPort, string.Join(", ", ports));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
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

        _logger.LogInformation("🛑 Serial Bridge Stopped");
    }
}