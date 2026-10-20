using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

app.MapGet("/api/telemetry", (TelemetryStateStore store) =>
{
    return Results.Ok(store.GetSnapshot());
});

app.UseFileServer();

app.Run();


// =====================================================
// Telemetry Snapshot
// =====================================================
public record TelemetrySnapshot(
    int RawValue,
    double Voltage,
    double Percentage,
    string DataSource,
    DateTime Timestamp
);


// =====================================================
// Telemetry State Store
// =====================================================
public class TelemetryStateStore
{
    private readonly object _lock = new();

    private int _rawValue;
    private double _voltage;
    private double _percentage;
    private string _dataSource = "Waiting for ESP32...";
    private DateTime _timestamp = DateTime.Now;

    public void Update(int rawValue, string source)
    {
        double percentage = (rawValue / 4095.0) * 100.0;
        percentage = Math.Clamp(percentage, 0.0, 100.0);

        double voltage = (rawValue / 4095.0) * 3.3;

        lock (_lock)
        {
            _rawValue = rawValue;
            _voltage = voltage;
            _percentage = percentage;
            _dataSource = source;
            _timestamp = DateTime.Now;
        }
    }

    public TelemetrySnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new TelemetrySnapshot(
                _rawValue,
                _voltage,
                _percentage,
                _dataSource,
                _timestamp
            );
        }
    }
}


// =====================================================
// Serial Bridge Worker
// =====================================================
public class SerialBridgeWorker : BackgroundService
{
    private readonly TelemetryStateStore _stateStore;
    private readonly ILogger<SerialBridgeWorker> _logger;

    private const string SelectedPort = "/dev/cu.usbserial-0001";
    private const int BaudRate = 115200;

    public SerialBridgeWorker(
        TelemetryStateStore stateStore,
        ILogger<SerialBridgeWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var serial = new SerialPort(
                    SelectedPort,
                    BaudRate
                );

                serial.ReadTimeout = 1000;
                serial.Open();

                _logger.LogInformation(
                    "Connected to ESP32: {Port}",
                    SelectedPort
                );

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        string line = serial.ReadLine().Trim();

                        _logger.LogInformation(
                            "Received: {Line}",
                            line
                        );

                        // รูปแบบข้อมูล:
                        // ADC:975,41509
                        if (line.StartsWith("ADC:"))
                        {
                            string[] parts =
                                line.Substring(4).Split(',');

                            if (parts.Length >= 1 &&
                                int.TryParse(
                                    parts[0],
                                    out int rawValue))
                            {
                                _stateStore.Update(
                                    rawValue,
                                    $"Live Hardware ({SelectedPort})"
                                );
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        // ไม่มีข้อมูลในช่วงเวลานี้
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Serial connection error: {Message}",
                    ex.Message
                );

                await Task.Delay(
                    2000,
                    stoppingToken
                );
            }
        }
    }
}