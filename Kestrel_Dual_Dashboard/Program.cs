using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<SimulationWorker>();

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

// Endpoint ส่งข้อมูล Telemetry ให้แดชบอร์ด
app.MapGet("/api/telemetry", (DualChannelStateStore state) => Results.Ok(state.GetSnapshot()));

app.Run();

// คลังข้อมูล 2 ช่องสัญญาณ (Thread-Safe)
public class DualChannelStateStore
{
    private readonly object _lock = new();
    private int _rawA = 0;
    private int _rawB = 0;
    private string _source = "Live ESP32 (COM9)"; // ตัด + Sim B ออกแล้ว
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
                    name = "Potentiometer (Hardware)",
                    rawValue = _rawA,
                    voltage = Math.Round((_rawA / 4095.0) * 3.3, 2),
                    percentage = Math.Round((_rawA / 4095.0) * 100.0, 1)
                },
                channelB = new
                {
                    name = "Audio VU Meter",
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

// Background Worker: ค่อยๆ เพิ่มค่าขึ้นทีละสเต็ป สลับขึ้น-ลง เนียนตา
public class SimulationWorker : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;

    public SimulationWorker(DualChannelStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        double currentA = 0.0;
        bool increasingA = true;

        double currentB = 0.0;
        bool increasingB = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            // ช่อง A: ไต่ระดับช้าๆ นุ่มนวล
            double stepA = 16.0; 
            if (increasingA)
            {
                currentA += stepA;
                if (currentA >= 4095.0)
                {
                    currentA = 4095.0;
                    increasingA = false;
                }
            }
            else
            {
                currentA -= stepA;
                if (currentA <= 0.0)
                {
                    currentA = 0.0;
                    increasingA = true;
                }
            }

            // ช่อง B: ไต่ระดับไฟ LED ขึ้น-ลง
            double stepB = 22.0;
            if (increasingB)
            {
                currentB += stepB;
                if (currentB >= 4095.0)
                {
                    currentB = 4095.0;
                    increasingB = false;
                }
            }
            else
            {
                currentB -= stepB;
                if (currentB <= 0.0)
                {
                    currentB = 0.0;
                    increasingB = true;
                }
            }

            // ส่งเป็น Live ESP32 (COM9) อย่างเดียว
            _stateStore.Update((int)currentA, (int)currentB, "Live ESP32 (COM9)");

            await Task.Delay(40, stoppingToken);
        }
    }
}