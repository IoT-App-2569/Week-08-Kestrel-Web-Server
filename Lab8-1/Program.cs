var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/led/{state}", (string state) => {
    string action = state.ToLower() == "on" ? "TURN ON 💡" : "TURN OFF 🌑";
    	Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LED Control: {state}");
    return Results.Ok(new { 
        device = "LED_D2", 
        requestedState = state, 
        actionResult = action,
        serverTime = DateTime.Now.ToString("HH:mm:ss")
    });
});
app.MapGet("/api/student", () => {
    return Results.Ok(new {
        studentId = "67030115", 
        studentName = "น้ำทิพย์ ป้อมหิน", 
        faculty = "คณะครุศาสตร์อุตสาหกรรมและเทคโนโลยี, สาขาเทคโนโลยีคอมพิวเตอร์", 
        targetSensor = "DHT22", 
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
          });
});
app.Run();