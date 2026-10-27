using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน Services สำหรับจัดการข้อมูลเบื้องหลัง
builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// 1. สั่งให้เซิร์ฟเวอร์แสดงหน้าเว็บ (index.html) ใน wwwroot อัตโนมัติ
app.UseDefaultFiles();
app.UseStaticFiles();

// 2. Endpoint สำหรับส่งข้อมูลให้หน้าเว็บ 7-Segment Retro
app.MapGet("/api/telemetry", (TelemetryStateStore state) => {
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();
    return Results.Ok(new {
        sensor = "ESP32-LDR",
        rawValue = raw,
        voltage = voltage,
        percentage = percent,
        dataSource = source,
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    });
});

app.Run();

// ============================================================================
// คลาสเก็บสถานะข้อมูล (State Store)
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
// คลาสสำหรับอ่านข้อมูลจาก ESP32 ผ่านสาย USB (พร้อมระบบจำลองข้อมูลถ้าสายหลุด)
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;

    public SerialBridgeWorker(TelemetryStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] availablePorts = SerialPort.GetPortNames();
            
            if (availablePorts.Length > 0)
            {
                string? targetPort = "COM3"; // กำหนดพอร์ตหลัก
                string? selectedPort = null;
                
                if (!string.IsNullOrEmpty(targetPort) && availablePorts.Contains(targetPort, StringComparer.OrdinalIgnoreCase))
                {
                    selectedPort = targetPort;
                }
                else
                {
                    selectedPort = availablePorts.FirstOrDefault(p => !p.Equals("COM1", StringComparison.OrdinalIgnoreCase)) ?? availablePorts[0];
                }

                try
                {
                    using var serial = new SerialPort(selectedPort, 115200);
                    serial.ReadTimeout = 2000;
                    serial.Open();
                    serial.DiscardInBuffer(); 

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
                        catch (TimeoutException) { await Task.Delay(50, stoppingToken); }
                    }
                }
                catch (Exception) { /* ข้ามเมื่อเปิดพอร์ตไม่ได้แล้วลงไปสลับใช้ Simulation Mode */ }
            }

            // โหมดจำลองข้อมูล (ถ้าไม่ได้เสียบสาย หรือโดนบล็อกพอร์ต มันจะปล่อย Sine Wave ให้ทดสอบหน้าเว็บได้ทันที)
            for (int i = 0; i < 20 && !stoppingToken.IsCancellationRequested; i++)
            {
                double t = Environment.TickCount64 / 1000.0;
                int simAdc = (int)((Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);
                _stateStore.Update(simAdc, "Simulation Mode (Sine Wave)");
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}