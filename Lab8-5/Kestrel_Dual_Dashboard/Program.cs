
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DualChannelStateStore>();
builder.Services.AddHostedService<DualSerialBridgeWorker>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () => Results.Ok(new
{
    gateway = "Kestrel Dual-Channel IoT Gateway",
    uptimeSeconds = Environment.TickCount64 / 1000,
    serverTime = DateTime.UtcNow.ToString("o")
}));

app.MapGet("/api/telemetry", (DualChannelStateStore state) =>
    Results.Ok(state.GetSnapshot()));

app.Run();

// ============================================================
// THREAD-SAFE STATE STORE: TWO CHANNELS
// ============================================================
public class DualChannelStateStore
{
    private readonly object _lock = new();

    private int _rawA;
    private int _rawB;
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
                    name = "Potentiometer (Hardware)",
                    rawValue = _rawA,
                    voltage = Math.Round(_rawA / 4095.0 * 3.3, 2),
                    percentage = Math.Round(_rawA / 4095.0 * 100.0, 1)
                },
                channelB = new
                {
                    name = "Sensor B (Simulated)",
                    rawValue = _rawB,
                    voltage = Math.Round(_rawB / 4095.0 * 3.3, 2),
                    percentage = Math.Round(_rawB / 4095.0 * 100.0, 1)
                },
                dataSource = _source,
                lastUpdated = _lastUpdated.ToString("o")
            };
        }
    }
}

// ============================================================
// SERIAL BRIDGE + SIMULATED CHANNEL B
// ============================================================
public class DualSerialBridgeWorker : BackgroundService
{
    private readonly DualChannelStateStore _state;
    private readonly ILogger<DualSerialBridgeWorker> _logger;

    public DualSerialBridgeWorker(
        DualChannelStateStore state,
        ILogger<DualSerialBridgeWorker> logger)
    {
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Let the web host finish starting before probing/opening serial hardware.
        await Task.Yield();

        // Start with a valid snapshot even before hardware connects.
        _state.Update(0, 0, "Initializing");

        while (!stoppingToken.IsCancellationRequested)
        {
            string? port = SerialPort.GetPortNames()
                .FirstOrDefault(p =>
                    p.Equals(
                        "/dev/cu.usbserial-0001",
                        StringComparison.OrdinalIgnoreCase))
                ?? SerialPort.GetPortNames().FirstOrDefault(p =>
                    p.Contains("usbserial", StringComparison.OrdinalIgnoreCase)
                    || p.Contains("usbmodem", StringComparison.OrdinalIgnoreCase)
                    || p.Contains("SLAB_USBtoUART", StringComparison.OrdinalIgnoreCase)
                    || p.Contains("wchusbserial", StringComparison.OrdinalIgnoreCase));

            if (port != null)
            {
                try
                {
                    using var serial = new SerialPort(port, 115200)
                    {
                        ReadTimeout = 1000
                    };

                    serial.Open();
                    serial.DiscardInBuffer();

                    _logger.LogInformation(
                        "Channel A connected to {Port}", port);

                    while (!stoppingToken.IsCancellationRequested
                           && serial.IsOpen)
                    {
                        try
                        {
                            string line = serial.ReadLine().Trim();

                            // Supports "1234" or "1234,2345".
                            string[] parts = line.Split(',');

                            if (int.TryParse(parts[0].Trim(), out int rawA)
                                && rawA >= 0 && rawA <= 4095)
                            {
                                double t =
                                    Environment.TickCount64 / 1000.0;

                                int simB = (int)(
                                    (Math.Sin(t * 1.2) + 1.0)
                                    / 2.0 * 4095);

                                string source =
                                    $"Live ESP32 ({port}) + Sim B";

                                if (parts.Length >= 2
                                    && int.TryParse(parts[1].Trim(), out int rawB)
                                    && rawB >= 0 && rawB <= 4095)
                                {
                                    simB = rawB;
                                    source = $"Live ESP32 ({port}) + Live B";
                                }

                                _state.Update(rawA, simB, source);

                                _logger.LogInformation(
                                    "A={A} ({AP:F1}%), B={B} ({BP:F1}%)",
                                    rawA, rawA / 4095.0 * 100.0,
                                    simB, simB / 4095.0 * 100.0);
                            }
                        }
                        catch (TimeoutException)
                        {
                            // No serial line arrived in time.
                        }

                        await Task.Delay(20, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Serial connection failed: {Message}",
                        ex.Message);
                }
            }
            else
            {
                _logger.LogWarning(
                    "No ESP32 serial port found; using simulation.");
            }

            // Simulation fallback when the hardware is unavailable.
            if (!stoppingToken.IsCancellationRequested)
            {
                for (int i = 0;
                     i < 20 && !stoppingToken.IsCancellationRequested;
                     i++)
                {
                    double t = Environment.TickCount64 / 1000.0;

                    int simA = (int)(
                        (Math.Sin(t * 0.8) + 1.0) / 2.0 * 4095);

                    int simB = (int)(
                        (Math.Cos(t * 1.4) + 1.0) / 2.0 * 4095);

                    _state.Update(
                        simA, simB,
                        "Simulation Mode (2 Channels)");

                    try
                    {
                        await Task.Delay(100, stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
    }
}
