using System.IO.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

app.MapGet(
    "/api/telemetry",
    (DualChannelStateStore state) =>
        Results.Ok(state.GetSnapshot())
);

app.Run();


// ============================================================
// Dual Channel State Store
// ============================================================

public class DualChannelStateStore
{
    private readonly object _lock = new();

    private int _rawA = 0;
    private int _rawB = 0;

    private string _source = "Initializing";

    private DateTime _lastUpdated =
        DateTime.UtcNow;

    public void Update(
        int rawA,
        int rawB,
        string source)
    {
        lock (_lock)
        {
            _rawA = Math.Clamp(rawA, 0, 4095);
            _rawB = Math.Clamp(rawB, 0, 4095);

            _source = source;

            _lastUpdated =
                DateTime.UtcNow;
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

                    voltage =
                        Math.Round(
                            (_rawA / 4095.0) * 3.3,
                            2
                        ),

                    percentage =
                        Math.Round(
                            (_rawA / 4095.0) * 100.0,
                            1
                        )
                },

                channelB = new
                {
                    name = "Sensor B (Simulated)",

                    rawValue = _rawB,

                    voltage =
                        Math.Round(
                            (_rawB / 4095.0) * 3.3,
                            2
                        ),

                    percentage =
                        Math.Round(
                            (_rawB / 4095.0) * 100.0,
                            1
                        )
                },

                dataSource = _source,

                lastUpdated =
                    _lastUpdated.ToString("o")
            };
        }
    }
}


// ============================================================
// Serial Bridge Worker
// ============================================================

public class DualSerialBridgeWorker
    : BackgroundService
{
    private readonly DualChannelStateStore _stateStore;

    private readonly ILogger<DualSerialBridgeWorker> _logger;

    // Mac USB Serial Port
    private const string SelectedPort =
        "/dev/cu.usbserial-0001";

    private const int BaudRate = 115200;


    public DualSerialBridgeWorker(
        DualChannelStateStore stateStore,
        ILogger<DualSerialBridgeWorker> logger)
    {
        _stateStore = stateStore;

        _logger = logger;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (
            !stoppingToken.IsCancellationRequested
        )
        {
            try
            {
                using var serial =
                    new SerialPort(
                        SelectedPort,
                        BaudRate
                    );

                serial.ReadTimeout = 1000;

                serial.DtrEnable = true;

                serial.RtsEnable = true;

                serial.Open();

                serial.DiscardInBuffer();


                _logger.LogInformation(
                    "Connected to ESP32: {Port}",
                    SelectedPort
                );


                while (
                    !stoppingToken.IsCancellationRequested
                    &&
                    serial.IsOpen
                )
                {
                    try
                    {
                        string line =
                            serial.ReadLine().Trim();


                        _logger.LogInformation(
                            "Received: {Line}",
                            line
                        );


                        // ==================================================
                        // ESP32 format:
                        //
                        // ADC:975,41509
                        //
                        // ADC = sensor value
                        // 41509 = timestamp
                        // ==================================================

                        if (
                            line.StartsWith(
                                "ADC:",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            string data =
                                line.Substring(4);

                            string[] parts =
                                data.Split(',');


                            if (
                                parts.Length >= 1
                                &&
                                int.TryParse(
                                    parts[0].Trim(),
                                    out int valueA
                                )
                            )
                            {
                                // ------------------------------------------
                                // Channel B Simulation
                                // ------------------------------------------

                                double time =
                                    Environment.TickCount64
                                    / 1000.0;


                                int simB =
                                    (int)
                                    (
                                        (
                                            Math.Sin(
                                                time * 1.2
                                            )
                                            + 1.0
                                        )
                                        / 2.0
                                        * 4095
                                    );


                                _stateStore.Update(
                                    valueA,
                                    simB,
                                    $"Live ({SelectedPort}) + Sim B"
                                );


                                _logger.LogInformation(
                                    "CH-A = {ValueA}, CH-B = {ValueB}",
                                    valueA,
                                    simB
                                );
                            }
                        }


                        // ==================================================
                        // รองรับกรณี ESP32 ส่งตัวเลขอย่างเดียว
                        //
                        // เช่น:
                        //
                        // 975
                        // ==================================================

                        else if (
                            int.TryParse(
                                line,
                                out int valueA
                            )
                        )
                        {
                            double time =
                                Environment.TickCount64
                                / 1000.0;


                            int simB =
                                (int)
                                (
                                    (
                                        Math.Sin(
                                            time * 1.2
                                        )
                                        + 1.0
                                    )
                                    / 2.0
                                    * 4095
                                );


                            _stateStore.Update(
                                valueA,
                                simB,
                                $"Live ({SelectedPort}) + Sim B"
                            );
                        }
                    }
                    catch (TimeoutException)
                    {
                        // ไม่มีข้อมูลเข้ามาภายใน ReadTimeout
                    }


                    await Task.Delay(
                        50,
                        stoppingToken
                    );
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