using System.IO.Ports;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TelemetryState>();
var app=builder.Build(); app.UseDefaultFiles(); app.UseStaticFiles();
app.MapGet("/api/student",()=>Results.Ok(new {studentId="67030154",studentName="Patcharapol",faculty="Faculty of Industrial Education and Technology, Computer Technology",targetSensor="DHT22",timestamp=DateTime.UtcNow}));
app.MapGet("/api/status",()=>Results.Ok(new {gateway="Kestrel Edge Gateway",studentId="67030154",mode="ready",time=DateTime.UtcNow}));
app.Run();
public sealed class TelemetryState { public int Raw=3268; public double Voltage=2.63; public double Percentage=79.8; }