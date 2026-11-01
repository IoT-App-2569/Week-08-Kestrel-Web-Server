using System.IO.Ports;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<DualSerialBridgeWorker>();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/api/status", () => Results.Ok(new { gateway = "Kestrel Dual-Channel IoT Gateway", studentId = "67030142", studentName = "Piyawit Pholsri", uptimeSeconds = Environment.TickCount64 / 1000, serverTime = DateTime.UtcNow.ToString("o") }));
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));
app.Run();

public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA;
    private int _rawB;
    private string _source = "Initializing";
    private DateTime _lastUpdated = DateTime.UtcNow;
    public void Update(int rawA, int rawB, string source) { lock (_lock) { _rawA = Math.Clamp(rawA, 0, 4095); _rawB = Math.Clamp(rawB, 0, 4095); _source = source; _lastUpdated = DateTime.UtcNow; } }
    public object GetSnapshot() { lock (_lock) { return new { channelA = new { name = "Sensor A (Hardware)", rawValue = _rawA, voltage = Math.Round((_rawA / 4095.0) * 3.3, 2), percentage = Math.Round((_rawA / 4095.0) * 100.0, 1) }, channelB = new { name = "Sensor B (Simulated / LDR)", rawValue = _rawB, voltage = Math.Round((_rawB / 4095.0) * 3.3, 2), percentage = Math.Round((_rawB / 4095.0) * 100.0, 1) }, dataSource = _source, lastUpdated = _lastUpdated.ToString("o") }; } }
}

public class DualSerialBridgeWorker(DualChannelStateStore state, ILogger<DualSerialBridgeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await Task.Yield();
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var serial = new SerialPort("COM3", 115200) { ReadTimeout = 2000, DtrEnable = true, RtsEnable = true };
                serial.Open(); serial.DiscardInBuffer(); logger.LogInformation("Connected to hardware COM3");
                while (!token.IsCancellationRequested && serial.IsOpen)
                {
                    if (serial.BytesToRead > 0)
                    {
                        var line = serial.ReadLine().Trim(); int rawA;
                        if (line.StartsWith('{')) { using var json = JsonDocument.Parse(line); rawA = json.RootElement.GetProperty("raw").GetInt32(); }
                        else if (!int.TryParse(line, out rawA)) { await Task.Delay(50, token); continue; }
                        var t = Environment.TickCount64 / 1000.0; var simB = (int)((Math.Sin(t * 1.2) + 1.0) / 2.0 * 4095);
                        state.Update(rawA, simB, "Live (COM3) + Sim B");
                    }
                    await Task.Delay(50, token);
                }
            }
            catch (Exception ex) { logger.LogWarning("COM3 unavailable: {Message}", ex.Message); await Task.Delay(1000, token); }
        }
    }
}
