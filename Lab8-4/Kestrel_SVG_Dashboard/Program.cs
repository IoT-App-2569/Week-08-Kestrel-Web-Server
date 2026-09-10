using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// กำหนด Port ให้รันที่ http://localhost:5000 หรือ http://localhost:5089
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});

// ลงทะเบียน State Store สำหรับเก็บค่า Telemetry ล่าสุด
builder.Services.AddSingleton<TelemetryStateStore>();
// ลงทะเบียน Background Service จำลองข้อมูล (Sine Wave Fallback)
builder.Services.AddHostedService<TelemetrySimulationWorker>();

var app = builder.Build();

// 1. เปิดใช้งาน Static Files Server เพื่อเสิร์ฟไฟล์ index.html ใน wwwroot
app.UseFileServer();

// 2. API Endpoint สำหรับดึงข้อมูล Telemetry
app.MapGet("/api/telemetry", (TelemetryStateStore store) =>
{
    return Results.Ok(store.CurrentTelemetry);
});

app.Run();

// ====================================================================
// Data Models & State Store
// ====================================================================
public class TelemetryData
{
    [JsonPropertyName("rawValue")]
    public int RawValue { get; set; }

    [JsonPropertyName("voltage")]
    public double Voltage { get; set; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }

    [JsonPropertyName("alertLevel")]
    public string AlertLevel { get; set; } = "NORMAL";

    [JsonPropertyName("dataSource")]
    public string DataSource { get; set; } = "Simulation Mode (Sine Wave)";
}

public class TelemetryStateStore
{
    public TelemetryData CurrentTelemetry { get; set; } = new TelemetryData();
}

// ====================================================================
// Background Worker: สร้างข้อมูลจำลองคลื่นไซน์ (Sine Wave)
// ====================================================================
public class TelemetrySimulationWorker : BackgroundService
{
    private readonly TelemetryStateStore _store;

    public TelemetrySimulationWorker(TelemetryStateStore store)
    {
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        double step = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            // คำนวณคลื่นไซน์ช่วง 0 - 4095
            double sineVal = (Math.Sin(step) + 1.0) / 2.0; // ค่าระหว่าง 0.0 - 1.0
            int raw = (int)(sineVal * 4095);
            double volt = (raw / 4095.0) * 3.3;
            double pct = (raw / 4095.0) * 100.0;

            string alert = "NORMAL";
            if (pct >= 85.0) alert = "CRITICAL";
            else if (pct >= 70.0) alert = "WARNING";

            _store.CurrentTelemetry = new TelemetryData
            {
                RawValue = raw,
                Voltage = Math.Round(volt, 2),
                Percentage = Math.Round(pct, 1),
                AlertLevel = alert,
                DataSource = "Simulation Mode (Sine Wave)"
            };

            step += 0.1; // ความเร็วของวงรอบคลื่นไซน์
            await Task.Delay(100, stoppingToken); // อัปเดตทุกๆ 100ms
        }
    }
}