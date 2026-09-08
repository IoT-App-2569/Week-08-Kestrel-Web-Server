using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ลงทะเบียน StateStore เป็น Singleton (มีชิ้นเดียวตลอดอายุโปรแกรม)
builder.Services.AddSingleton<TelemetryStateStore>();
// ลงทะเบียน SerialBridgeWorker เป็น Background Hosted Service
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// Endpoint หน้าแรก
app.MapGet("/", () => "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

// Endpoint สำหรับดึงค่า Telemetry ล่าสุด (พร้อมภารกิจท้าทาย alertLevel)
app.MapGet("/api/telemetry", (TelemetryStateStore state) => {
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();
    
    // 🎯 ภารกิจท้าทาย (Micro-Challenge): กำหนดเงื่อนไข alertLevel
    string alertLevel = percent switch
    {
        > 85.0 => "DANGER (HIGH)",
        >= 70.0 => "WARNING",
        _ => "NORMAL"
    };

    return Results.Ok(new {
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

// ============================================================================
// 1. THREAD-SAFE STATE STORE (คลังเก็บค่าสถานะกลาง)
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
// BACKGROUND WORKER (จำลองค่า ADC ตั้งแต่ 0 - 4095 เพื่อทดสอบ Alert Level)
// ============================================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;

    public SerialBridgeWorker(TelemetryStateStore stateStore, ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        int mockAdc = 0;
        int step = 200; // ปรับเพิ่มทีละประมาณ 5% เพื่อให้เห็นการเปลี่ยนแปลงชัดเจน

        while (!stoppingToken.IsCancellationRequested)
        {
            _stateStore.Update(mockAdc, "Simulation Mode (Test Sweep)");
            
            mockAdc += step;
            if (mockAdc > 4095) mockAdc = 0; // เมื่อถึงค่าสูงสุดให้เริ่มใหม่ที่ 0

            await Task.Delay(1500, stoppingToken); // หน่วงเวลา 1.5 วินาทีต่อการเปลี่ยนค่า
        }
    }
}