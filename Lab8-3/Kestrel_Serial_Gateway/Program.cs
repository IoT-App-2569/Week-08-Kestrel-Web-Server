using System.IO.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryStateStore>();
builder.Services.AddHostedService<SerialBridgeWorker>();

var app = builder.Build();

app.MapGet("/", () => "IoT Edge Gateway Online! Visit /api/telemetry to view live data.");

app.MapGet("/api/telemetry", (TelemetryStateStore state) =>
{
	var (raw, voltage, percent, source, updated) = state.GetSnapshot();
	var alertLevel = percent switch
	{
		> 85.0 => "DANGER (HIGH)",
		>= 70.0 => "WARNING",
		_ => "NORMAL"
	};

	return Results.Ok(new
	{
		sensor = "ESP32-Potentiometer",
		rawValue = raw,
		voltage,
		percentage = percent,
		alertLevel,
		dataSource = source,
		timestamp = updated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
	});
});

app.Run();

public sealed class TelemetryStateStore
{
	private readonly object _lock = new();
	private int _rawValue;
	private DateTime _lastUpdated = DateTime.UtcNow;
	private string _source = "Initializing";

	public void Update(int rawValue, string source)
	{
		lock (_lock)
		{
			_rawValue = Math.Clamp(rawValue, 0, 4095);
			_source = source;
			_lastUpdated = DateTime.UtcNow;
		}
	}

	public (int raw, double voltage, double percent, string source, DateTime updated) GetSnapshot()
	{
		lock (_lock)
		{
			var voltage = Math.Round((_rawValue / 4095.0) * 3.3, 2);
			var percent = Math.Round((_rawValue / 4095.0) * 100.0, 1);
			return (_rawValue, voltage, percent, _source, _lastUpdated);
		}
	}
}

public sealed class SerialBridgeWorker : BackgroundService
{
	private readonly TelemetryStateStore _stateStore;
	private readonly ILogger<SerialBridgeWorker> _logger;
	private readonly string? _configuredPort;

	public SerialBridgeWorker(
		TelemetryStateStore stateStore,
		ILogger<SerialBridgeWorker> logger,
		IConfiguration configuration)
	{
		_stateStore = stateStore;
		_logger = logger;
		_configuredPort = configuration["SerialPort"] ?? Environment.GetEnvironmentVariable("SERIAL_PORT");
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			await Task.Yield();

			while (!stoppingToken.IsCancellationRequested)
		{
			var availablePorts = SerialPort.GetPortNames();
			var selectedPort = SelectPort(availablePorts);
			var liveMode = false;

			if (selectedPort is not null)
			{
				_logger.LogInformation("กำลังทดลองเชื่อมต่อพอร์ต: {Port}", selectedPort);

				try
				{
					using var serial = new SerialPort(selectedPort, 115200)
					{
						ReadTimeout = 2000
					};
					serial.Open();
					serial.DiscardInBuffer();
					liveMode = true;
					_logger.LogInformation("เชื่อมต่อฮาร์ดแวร์สำเร็จบน {Port} (Live Mode)", selectedPort);

					while (!stoppingToken.IsCancellationRequested && serial.IsOpen)
					{
						try
						{
							if (serial.BytesToRead > 0)
							{
								var line = serial.ReadLine().Trim();
								if (int.TryParse(line, out var value))
								{
									_stateStore.Update(value, $"Live Hardware ({selectedPort})");
								}
							}
							else
							{
								await Task.Delay(50, stoppingToken);
							}
						}
						catch (TimeoutException)
						{
							await Task.Delay(50, stoppingToken);
						}
					}
				}
				catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
				{
					_logger.LogWarning("ไม่สามารถเปิด {Port}: {Message} จึงสลับเข้าโหมดจำลอง", selectedPort, exception.Message);
				}
			}
			else
			{
				_logger.LogInformation("ไม่พบพอร์ต USB จึงทำงานในโหมดจำลอง");
			}

			if (!liveMode)
			{
				for (var index = 0; index < 20 && !stoppingToken.IsCancellationRequested; index++)
				{
					var time = Environment.TickCount64 / 1000.0;
					var simulatedValue = (int)((Math.Sin(time * 1.5) + 1.0) / 2.0 * 4095);
					_stateStore.Update(simulatedValue, "Simulation Mode (Sine Wave)");
					await Task.Delay(100, stoppingToken);
				}
			}
		}
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
			_logger.LogInformation("Serial bridge stopped.");
		}
	}

	private string? SelectPort(string[] availablePorts)
	{
		if (!string.IsNullOrWhiteSpace(_configuredPort))
		{
			var configuredMatch = availablePorts.FirstOrDefault(
				port => port.Equals(_configuredPort, StringComparison.OrdinalIgnoreCase));

			if (configuredMatch is not null)
			{
				return configuredMatch;
			}

			_logger.LogWarning("ไม่พบพอร์ตที่กำหนดไว้ {Port} จึงเลือกพอร์ตอัตโนมัติ", _configuredPort);
		}

		return availablePorts.FirstOrDefault(
			port => !port.Equals("COM1", StringComparison.OrdinalIgnoreCase));
	}
}
