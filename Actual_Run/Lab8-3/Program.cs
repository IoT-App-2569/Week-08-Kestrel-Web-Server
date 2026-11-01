using System.Collections.Concurrent;
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();
var app = builder.Build();

app.MapGet("/api/telemetry", (TelemetryStateStore state) => {
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();
    string alertLevel = percent > 85.0 ? "DANGER (HIGH)" : percent >= 70.0 ? "WARNING" : "NORMAL";
    return Results.Ok(new {
        sensor = "ESP32-Potentiometer", rawValue = raw, voltage,
        percentage = percent, dataSource = source,
        timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), alertLevel
    });
});
app.UseFileServer();
app.Run();

public class TelemetryStateStore
{
    private readonly object _lock = new();
    private int _raw; private double _voltage; private double _percentage;
    private string _source = "Waiting"; private DateTime _updated = DateTime.UtcNow;
    public void Update(int raw, string source) { lock (_lock) { _raw=raw; _voltage=raw/4095.0*3.3; _percentage=raw/4095.0*100.0; _source=source; _updated=DateTime.UtcNow; } }
    public (int,double,double,string,DateTime) GetSnapshot() { lock (_lock) return (_raw,_voltage,_percentage,_source,_updated); }
}

public class SerialBridgeWorker(TelemetryStateStore state, ILogger<SerialBridgeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var serial = new SerialPort("COM3", 115200) { NewLine = "\n", ReadTimeout = 1000 };
                serial.Open();
                logger.LogInformation("เชื่อมต่อฮาร์ดแวร์ COM3 สำเร็จ");
                while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
                {
                    try
                    {
                        string line = serial.ReadLine().Trim();
                        using var doc = System.Text.Json.JsonDocument.Parse(line);
                        int raw = doc.RootElement.GetProperty("raw").GetInt32();
                        state.Update(raw, "Hardware Serial (COM3)");
                    }
                    catch (TimeoutException) { }
                    catch (System.Text.Json.JsonException) { }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("เปิด COM3 ไม่สำเร็จ: {Message}", ex.Message);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
