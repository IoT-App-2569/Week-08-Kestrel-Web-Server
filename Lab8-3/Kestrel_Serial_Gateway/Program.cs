using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
// Services
// ================================================================

builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

// ================================================================
// Live Web Page
// ================================================================

app.MapGet("/", () =>
{
    return Results.Content(
        """
        <!DOCTYPE html>
        <html lang="en">

        <head>
            <meta charset="UTF-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>ESP32 Telemetry Dashboard</title>

            <style>
                body {
                    font-family: Arial, sans-serif;
                    background: #f4f6f8;
                    margin: 0;
                    padding: 40px;
                }

                .container {
                    max-width: 700px;
                    margin: auto;
                }

                h1 {
                    text-align: center;
                    margin-bottom: 30px;
                }

                .card {
                    background: white;
                    border-radius: 12px;
                    padding: 25px;
                    margin-bottom: 15px;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.08);
                }

                .label {
                    color: #666;
                    font-size: 14px;
                    margin-bottom: 8px;
                }

                .value {
                    font-size: 32px;
                    font-weight: bold;
                }

                .source {
                    font-size: 16px;
                    color: #333;
                    word-break: break-all;
                }

                .status {
                    text-align: center;
                    margin-top: 20px;
                    color: #555;
                }
            </style>
        </head>

        <body>

            <div class="container">

                <h1>ESP32 Telemetry Dashboard</h1>

                <!-- Raw ADC -->
                <div class="card">
                    <div class="label">
                        Raw ADC Value
                    </div>

                    <div class="value"
                         id="rawValue">
                        -
                    </div>
                </div>

                <!-- Voltage -->
                <div class="card">
                    <div class="label">
                        Voltage
                    </div>

                    <div class="value">
                        <span id="voltage">-</span> V
                    </div>
                </div>

                <!-- Percentage -->
                <div class="card">
                    <div class="label">
                        Percentage
                    </div>

                    <div class="value">
                        <span id="percentage">-</span> %
                    </div>
                </div>

                <!-- Alert Level -->
                <div class="card">
                    <div class="label">
                        Alert Level
                    </div>

                    <div class="value"
                         id="alertLevel">
                        -
                    </div>
                </div>

                <!-- Data Source -->
                <div class="card">
                    <div class="label">
                        Data Source
                    </div>

                    <div class="source"
                         id="source">
                        -
                    </div>
                </div>

                <!-- Last Update -->
                <div class="status">
                    Last update:
                    <span id="lastUpdate">-</span>
                </div>

            </div>

            <script>

                async function updateTelemetry() {

                    try {

                        const response =
                            await fetch('/api/telemetry');

                        if (!response.ok) {
                            throw new Error(
                                'HTTP error: ' +
                                response.status
                            );
                        }

                        const data =
                            await response.json();

                        // ============================================
                        // Raw ADC
                        // ============================================

                        document.getElementById(
                            'rawValue'
                        ).textContent =
                            data.rawValue;

                        // ============================================
                        // Voltage
                        // ============================================

                        document.getElementById(
                            'voltage'
                        ).textContent =
                            Number(
                                data.voltage
                            ).toFixed(2);

                        // ============================================
                        // Percentage
                        // ============================================

                        document.getElementById(
                            'percentage'
                        ).textContent =
                            Number(
                                data.percentage
                            ).toFixed(1);

                        // ============================================
                        // Alert Level
                        // ============================================

                        document.getElementById(
                            'alertLevel'
                        ).textContent =
                            data.alertLevel;

                        // ============================================
                        // Data Source
                        // ============================================

                        document.getElementById(
                            'source'
                        ).textContent =
                            data.dataSource;

                        // ============================================
                        // Last Update
                        // ============================================

                        document.getElementById(
                            'lastUpdate'
                        ).textContent =
                            new Date().toLocaleTimeString();

                    }
                    catch (error) {

                        console.error(
                            'ไม่สามารถอ่านข้อมูลได้:',
                            error
                        );

                    }
                }

                // โหลดข้อมูลทันที
                updateTelemetry();

                // อัปเดตข้อมูลทุก 500 ms
                setInterval(
                    updateTelemetry,
                    500
                );

            </script>

        </body>

        </html>
        """,
        "text/html; charset=utf-8"
    );
});

// ================================================================
// API: Telemetry
// ================================================================

app.MapGet(
    "/api/telemetry",
    (TelemetryStateStore store) =>
    {
        return Results.Ok(
            store.GetSnapshot()
        );
    }
);

// ================================================================
// Run
// ================================================================

app.Run();

// ================================================================
// Telemetry State Store
// ================================================================

public record TelemetrySnapshot(
    string Sensor,
    int RawValue,
    double Voltage,
    double Percentage,
    string AlertLevel,
    string DataSource,
    DateTime Timestamp
);

public class TelemetryStateStore
{
    private readonly object _lock = new();

    private TelemetrySnapshot _current =
        new TelemetrySnapshot(
            "ESP32-Potentiometer",
            0,
            0.0,
            0.0,
            "NORMAL",
            "Initializing",
            DateTime.UtcNow
        );

    public void Update(
        int rawValue,
        string dataSource
    )
    {
        // ============================================================
        // ESP32 ADC 12-bit
        // Range = 0 - 4095
        // ============================================================

        double voltage =
            rawValue / 4095.0 * 3.3;

        double percentage =
            rawValue / 4095.0 * 100.0;

        // ============================================================
        // Alert Level
        // ============================================================

        string alertLevel;

        if (percentage > 85.0)
        {
            alertLevel = "DANGER (HIGH)";
        }
        else if (percentage >= 70.0)
        {
            alertLevel = "WARNING";
        }
        else
        {
            alertLevel = "NORMAL";
        }

        // ============================================================
        // Update Current State
        // ============================================================

        lock (_lock)
        {
            _current =
                new TelemetrySnapshot(
                    "ESP32-Potentiometer",

                    rawValue,

                    Math.Round(
                        voltage,
                        2
                    ),

                    Math.Round(
                        percentage,
                        1
                    ),

                    alertLevel,

                    dataSource,

                    DateTime.UtcNow
                );
        }
    }

    public TelemetrySnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return _current;
        }
    }
}

// ================================================================
// Serial Bridge Worker
// ================================================================

public class SerialBridgeWorker : BackgroundService
{
    private readonly ILogger<SerialBridgeWorker> _logger;

    private readonly TelemetryStateStore _stateStore;

    public SerialBridgeWorker(
        ILogger<SerialBridgeWorker> logger,
        TelemetryStateStore stateStore
    )
    {
        _logger = logger;
        _stateStore = stateStore;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    )
    {
        // ============================================================
        // Serial Configuration
        // ============================================================

        string selectedPort =
            "/dev/cu.usbserial-0001";

        int baudRate = 115200;

        _logger.LogInformation(
            "🔌 Serial Bridge Starting..."
        );

        _logger.LogInformation(
            "📡 Target Port: {Port}",
            selectedPort
        );

        _logger.LogInformation(
            "⚡ Baud Rate: {BaudRate}",
            baudRate
        );

        // ============================================================
        // Main Loop
        // ============================================================

        while (
            !stoppingToken.IsCancellationRequested
        )
        {
            try
            {
                // ========================================================
                // Check Serial Port
                // ========================================================

                if (
                    SerialPort.GetPortNames()
                        .Contains(selectedPort)
                )
                {
                    _logger.LogInformation(
                        "✅ พบพอร์ต USB: {Port}",
                        selectedPort
                    );

                    using SerialPort serial =
                        new SerialPort(
                            selectedPort,
                            baudRate
                        );

                    serial.NewLine = "\n";

                    serial.ReadTimeout = 1000;

                    serial.Open();

                    _logger.LogInformation(
                        "🟢 Serial Port เปิดสำเร็จ"
                    );

                    _logger.LogInformation(
                        "📥 กำลังรอข้อมูลจาก ESP32..."
                    );

                    // ====================================================
                    // Read Serial Data
                    // ====================================================

                    while (
                        !stoppingToken.IsCancellationRequested
                    )
                    {
                        try
                        {
                            string line =
                                serial.ReadLine().Trim();

                            if (
                                string.IsNullOrWhiteSpace(
                                    line
                                )
                            )
                            {
                                continue;
                            }

                            _logger.LogInformation(
                                "📥 Received: {Line}",
                                line
                            );

                            // =================================================
                            // ESP32 sends:
                            //
                            // ADC:975,41509
                            //
                            // Format:
                            // ADC:<raw>,<timestamp>
                            // =================================================

                            if (
                                line.StartsWith(
                                    "ADC:"
                                )
                            )
                            {
                                string[] parts =
                                    line
                                        .Substring(4)
                                        .Split(',');

                                if (
                                    parts.Length >= 1 &&
                                    int.TryParse(
                                        parts[0],
                                        out int val
                                    )
                                )
                                {
                                    _stateStore.Update(
                                        val,
                                        $"Live Hardware ({selectedPort})"
                                    );

                                    _logger.LogInformation(
                                        "📊 ADC = {Value}",
                                        val
                                    );
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "⚠️ ไม่สามารถแปลงค่า ADC: {Line}",
                                        line
                                    );
                                }
                            }
                            else
                            {
                                _logger.LogDebug(
                                    "ℹ️ ข้ามข้อมูลที่ไม่ใช่ ADC: {Line}",
                                    line
                                );
                            }
                        }
                        catch (TimeoutException)
                        {
                            // ไม่มีข้อมูลในช่วงเวลานี้
                            // วนกลับมาอ่านใหม่
                        }
                    }

                    serial.Close();

                    _logger.LogInformation(
                        "🔴 Serial Port ถูกปิด"
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "🔍 ไม่พบพอร์ต USB: {Port}",
                        selectedPort
                    );

                    _logger.LogInformation(
                        "📋 Available Ports: {Ports}",
                        string.Join(
                            ", ",
                            SerialPort.GetPortNames()
                        )
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Serial Bridge Error"
                );
            }

            // ============================================================
            // Retry every 2 seconds
            // ============================================================

            try
            {
                await Task.Delay(
                    2000,
                    stoppingToken
                );
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "🛑 Serial Bridge Stopped"
        );
    }
}