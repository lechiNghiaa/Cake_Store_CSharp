
var builder = WebApplication.CreateBuilder(args);

// Đăng ký Controllers
builder.Services.AddControllers();

// Đăng ký Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Bật Swagger trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API kiểm tra trạng thái hoạt động
app.MapGet("/health/live", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "TiemBanh.Api",
        stage = "development"
    });
})
.WithName("HealthCheck")
.WithTags("Health");

app.MapControllers();

app.Run();

// Hỗ trợ Integration Test
public partial class Program { }
