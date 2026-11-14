// Web khởi đầu. Bổ sung YARP /api/ và giao diện theo UC01–UC15 ở các commit sau.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", stage = "scaffold" }));
app.Run();
