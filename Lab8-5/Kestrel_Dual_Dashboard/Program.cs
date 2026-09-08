using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<PureSimulationWorker>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway (Simulation Mode)",
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o")
}));

app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// ============================================================================
// THREAD-SAFE DUAL-CHANNEL STATE STORE
// ============================================================================
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _source = "Initializing";
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
                    name = "Sensor A (Simulated)",
                    rawValue = _rawA,
                    voltage = Math.Round((_rawA / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawA / 4095.0) * 100.0, 1)
                },
                channelB = new
                {
                    name = "Sensor B (Simulated)",
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

// ============================================================================
// BACKGROUND WORKER (PURE SIMULATION)
// ============================================================================
public class PureSimulationWorker : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;
    private readonly ILogger<PureSimulationWorker> _logger;

    public PureSimulationWorker(DualChannelStateStore stateStore, ILogger<PureSimulationWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _logger.LogInformation("🔄 เปิดใช้งาน Pure Simulation Sweep Mode");

        while (!stoppingToken.IsCancellationRequested)
        {
            double time = Environment.TickCount64 / 1000.0;

            // สร้างคลื่น Sine สำหรับ Channel A และ Cosine สำหรับ Channel B ให้ขยับต่างกัน
            int simA = (int)((Math.Sin(time * 1.5) + 1.0) / 2.0 * 4095);
            int simB = (int)((Math.Cos(time * 2.0) + 1.0) / 2.0 * 4095);

            _stateStore.Update(simA, simB, "Simulation Mode (Pure Sweep)");

            await Task.Delay(80, stoppingToken);
        }
    }
}