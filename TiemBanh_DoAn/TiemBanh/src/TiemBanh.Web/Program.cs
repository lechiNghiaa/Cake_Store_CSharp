// ==================== PRESENTATION — WEB VÀ REVERSE PROXY ====================
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app=builder.Build();
// Danh sách bánh / Giỏ hàng và đặt COD / Xử lý đơn hàng — phục vụ HTML, CSS, JS.
app.UseDefaultFiles();app.UseStaticFiles();app.MapReverseProxy();app.MapGet("/health/live",()=>Results.Ok(new{status="healthy"}));app.Run();
