var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Welcome to IoT Edge Gateway by Pornprom Sukjaimit!");

app.MapGet("/api/student", () => new {
    studentId = "67030323",
    studentName = "Pornprom Sukjaimit",
    faculty = "Computer Education",
    targetSensor = "Relay Module",
    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
});

app.Run();
