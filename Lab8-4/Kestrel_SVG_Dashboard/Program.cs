
using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// Register State Store and Background Worker
builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// Serve index.html and other static files from wwwroot
app.UseFileServer();

// API for reading sensor telemetry
app.MapGet("/api/telemetry", (TelemetryStateStore state) =>
{
    var (raw, voltage, percent, source, updated) = state.GetSnapshot();

    string alertLevel = percent > 85.0
        ? "DANGER (HIGH)"
        : percent >= 70.0
            ? "WARNING"
            : "NORMAL";

    return Results.Ok(new
    {
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

// ============================================================
// 1. THREAD-SAFE STATE STORE
// ============================================================
public class TelemetryStateStore
{
    private readonly object _lock = new();

    private int _rawValue = 0;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private string _source = "Initializing";

    // ADC calibration range for percentage calculation.
    // Change ADC_MIN and ADC_MAX after testing your potentiometer.
    private const int ADC_MIN = 0;
    private const int ADC_MAX = 4095;

    public void Update(int rawValue, string source)
    {
        lock (_lock)
        {
            _rawValue = Math.Clamp(rawValue, 0, 4095);
            _source = source;
            _lastUpdated = DateTime.UtcNow;
        }
    }

    public (int raw, double voltage, double percent,
        string source, DateTime updated) GetSnapshot()
    {
        lock (_lock)
        {
            // Voltage is based on the raw 12-bit ADC value.
            double voltage = Math.Round(
                (_rawValue / 4095.0) * 3.3, 2);

            // Map the calibrated ADC range to 0–100%.
            double percent;

            if (ADC_MAX <= ADC_MIN)
            {
                percent = 0;
            }
            else
            {
                percent = ((_rawValue - ADC_MIN)
                    / (double)(ADC_MAX - ADC_MIN)) * 100.0;

                percent = Math.Clamp(percent, 0.0, 100.0);
            }

            percent = Math.Round(percent, 1);

            return (_rawValue, voltage, percent, _source, _lastUpdated);
        }
    }
}

// ============================================================
// 2. BACKGROUND WORKER: SERIAL BRIDGE + SIMULATION
// ============================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;

    public SerialBridgeWorker(
        TelemetryStateStore stateStore,
        ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    private void PrintTelemetry(int rawValue, string source)
    {
        double voltage = (rawValue / 4095.0) * 3.3;

        // Display the raw ADC percentage in the log.
        double percent = (rawValue / 4095.0) * 100.0;

        string alertLevel = percent > 85.0
            ? "DANGER (HIGH)"
            : percent >= 70.0
                ? "WARNING"
                : "NORMAL";

        Console.WriteLine(
            $"[{source}] ADC: {rawValue,4} | " +
            $"Voltage: {voltage:F2} V | " +
            $"Level: {percent:F1}% | " +
            $"Alert: {alertLevel}");
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            string[] ports = SerialPort.GetPortNames();

            string? selectedPort = ports.FirstOrDefault(p =>
                p.Equals(
                    "/dev/cu.usbserial-0001",
                    StringComparison.OrdinalIgnoreCase));

            selectedPort ??= ports.FirstOrDefault(p =>
                p.Contains("usbserial", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("SLAB_USBtoUART", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("wchusbserial", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("usbmodem", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("COM", StringComparison.OrdinalIgnoreCase));

            bool hardwareConnected = false;

            if (selectedPort != null)
            {
                try
                {
                    _logger.LogInformation(
                        "Connecting to port {Port}", selectedPort);

                    using var serial = new SerialPort(
                        selectedPort, 115200)
                    {
                        ReadTimeout = 2000
                    };

                    serial.Open();
                    serial.DiscardInBuffer();

                    hardwareConnected = true;

                    _logger.LogInformation(
                        "Hardware connected on {Port} (Live Mode)",
                        selectedPort);

                    while (!stoppingToken.IsCancellationRequested
                           && serial.IsOpen)
                    {
                        try
                        {
                            string line = serial.ReadLine().Trim();

                            if (int.TryParse(line, out int value)
                                && value >= 0 && value <= 4095)
                            {
                                _stateStore.Update(
                                    value,
                                    $"Live Hardware ({selectedPort})");

                                PrintTelemetry(
                                    value,
                                    $"ESP32 {selectedPort}");
                            }
                        }
                        catch (TimeoutException)
                        {
                            // Continue waiting for serial data.
                        }
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
                        "Could not open or read port {Port}: {Message}",
                        selectedPort, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation(
                    "No serial port found. Using Simulation Mode.");
            }

            // Simulate data only when hardware is not connected.
            if (!hardwareConnected)
            {
                for (int i = 0;
                     i < 20 && !stoppingToken.IsCancellationRequested;
                     i++)
                {
                    double t = Environment.TickCount64 / 1000.0;

                    int simAdc = (int)(
                        (Math.Sin(t * 1.5) + 1.0) / 2.0 * 4095);

                    _stateStore.Update(
                        simAdc,
                        "Simulation Mode (Sine Wave)");

                    PrintTelemetry(simAdc, "SIMULATION");

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
