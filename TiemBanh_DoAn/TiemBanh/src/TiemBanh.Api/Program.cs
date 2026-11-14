// Khung API: sẽ bổ sung xác thực JWT, DI Business, Controllers và Error Middleware.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", stage = "scaffold" }));
app.MapControllers();
app.Run();
public partial class Program { }
