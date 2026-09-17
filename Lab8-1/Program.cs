var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "sawadeka Welcome to IoT Edge Gateway by paphatsorn-67030311!");

   app.MapGet("/api/student", () => new {
       StudentId = "67030311",
       StudentName = "Paphatsorn Aermsaarrd",
       faculty = "ครุศาสตร์วิศวกรรมศาสตร์สาขาเทคโนโลยีคอมพิมวเตอร์",
       targetSensor = "DHT22", 
       timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
});

app.Run();
