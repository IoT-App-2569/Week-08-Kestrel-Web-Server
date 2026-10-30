using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// 1. ลงทะเบียน State Store และ Background Worker
builder.Services.AddSingleton<SensorStateStore>();
builder.Services.AddHostedService<SerialGatewayWorker>();

var app = builder.Build();

// 2. สร้าง API Endpoint สำหรับให้ index.html ดึงข้อมูลแบบ Polling
app.MapGet("/api/telemetry", (SensorStateStore store) =>
{
    return Results.Ok(store.GetLatest());
});

// 3. เปิดใช้งานการเสิร์ฟไฟล์ Static (wwwroot/index.html)
app.UseFileServer();

app.Run();

// =========================================================================
// คลาสสำหรับเก็บข้อมูลเซนเซอร์ (State Store)
// =========================================================================
public class TelemetryData
{
    public int RawValue { get; set; }
    public double Voltage { get; set; }
    public double Percentage { get; set; }
    public string DataSource { get; set; } = "ESP32 (Serial)";
}

public class SensorStateStore
{
    private TelemetryData _latest = new TelemetryData();
    private readonly object _lock = new object();

    public TelemetryData GetLatest()
    {
        lock (_lock)
        {
            return _latest;
        }
    }

    public void Update(int raw, string source = "ESP32 (COM)")
    {
        lock (_lock)
        {
            double voltage = (raw / 4095.0) * 3.3;
            double pct = (raw / 4095.0) * 100.0;

            _latest = new TelemetryData
            {
                RawValue = raw,
                Voltage = Math.Round(voltage, 2),
                Percentage = Math.Round(pct, 1),
                DataSource = source
            };
        }
    }
}

// =========================================================================
// Background Worker สำหรับอ่านค่า Serial Port จาก ESP32
// =========================================================================
public class SerialGatewayWorker : BackgroundService
{
    private readonly SensorStateStore _store;
    private readonly ILogger<SerialGatewayWorker> _logger;

    public SerialGatewayWorker(SensorStateStore store, ILogger<SerialGatewayWorker> logger)
    {
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string portName = "COM3"; // ** ปรับให้ตรงกับ COM Port ของ ESP32 คุณ **
        SerialPort? serialPort = null;

        try
        {
            serialPort = new SerialPort(portName, 115200);
            serialPort.Open();
            _logger.LogInformation("เชื่อมต่อกับ Serial Port {Port}สำเร็จ", portName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("ไม่สามารถเปิด Serial Port {Port} ได้ ({Message}) เข้าสู่โหมดจำลองข้อมูล (Simulation Mode)", portName, ex.Message);
        }

        int simValue = 0;
        bool incrementing = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        string line = serialPort.ReadLine().Trim();
                        if (int.TryParse(line, out int raw))
                        {
                            _store.Update(raw, $"ESP32 ({portName})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "เกิดข้อผิดพลาดในการอ่านค่าจาก Serial Port");
                }
            }
            else
            {
                // โหมดจำลองข้อมูล (ถ้าไม่ได้เสียบสาย ESP32 หรือ COM Port ไม่ถูกต้อง)
                simValue += incrementing ? 100 : -100;
                if (simValue >= 4095) { simValue = 4095; incrementing = false; }
                if (simValue <= 0) { simValue = 0; incrementing = true; }

                _store.Update(simValue, "Simulated Stream");
            }

            await Task.Delay(150, stoppingToken);
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}