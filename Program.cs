
var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký Controllers
builder.Services.AddControllers();

// 2. Đăng ký Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Đăng ký xử lý lỗi theo chuẩn Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // Thêm mã truy vết để tra cứu lỗi trong log
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;

        // Không hiển thị đường dẫn hoặc chi tiết exception nhạy cảm
        if (context.ProblemDetails.Status == 500)
        {
            context.ProblemDetails.Title =
                "Internal Server Error";

            context.ProblemDetails.Detail =
                "Hệ thống xảy ra lỗi. Vui lòng thử lại sau.";
        }
    };
});

var app = builder.Build();

// 4. Xử lý ngoại lệ tập trung
app.UseExceptionHandler();

// 5. Chuẩn hóa phản hồi cho HTTP status không có body
app.UseStatusCodePages();

// 6. Swagger chỉ chạy trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Endpoint phục vụ kiểm thử lỗi, không mở ở Production
    app.MapGet("/dev/test-error", () =>
    {
        throw new InvalidOperationException(
            "Loi gia lap de kiem tra Exception Handler");
    })
    .ExcludeFromDescription();
}

// 7. Health Check
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

// 8. Ánh xạ Controllers
app.MapControllers();

app.Run();

public partial class Program { }
