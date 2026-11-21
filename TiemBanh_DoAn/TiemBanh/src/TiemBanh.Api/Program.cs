using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TiemBanh.Business;
using TiemBanh.DataAccess;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
var key = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Thiếu Jwt__SigningKey.");
if (Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("JWT key phải có ít nhất 32 byte.");
builder.Services.AddBakery(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true, ValidIssuer = "TiemBanh",
        ValidateAudience = true, ValidAudience = "TiemBanhClients",
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(15)
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var id = int.Parse(ctx.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var stamp = ctx.Principal!.FindFirstValue("stamp");
            var store = ctx.HttpContext.RequestServices.GetRequiredService<BakeryStore>();
            if (!await store.Query<AppUser>().AnyAsync(x => x.Id == id && x.Active && x.SecurityStamp == stamp))
                ctx.Fail("Tài khoản đã khóa hoặc phiên không còn hợp lệ.");
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.Use(async(ctx,next)=>{
 try {await next();}
 catch(Exception ex){
  var status=ex is BusinessException b?b.Status:ex is DbUpdateConcurrencyException?409:ex is DbUpdateException?409:ex is Microsoft.Data.SqlClient.SqlException sql&&sql.Number==1205?409:500;
  var code=ex is BusinessException e?e.Code:status==409?"DATA_CONFLICT":"INTERNAL_ERROR";
  app.Logger.LogError(ex,"Request failed {TraceId} {Code}",ctx.TraceIdentifier,code);
  if(!ctx.Response.HasStarted){ctx.Response.StatusCode=status;await ctx.Response.WriteAsJsonAsync(new{type="urn:tiembanh:"+code.ToLowerInvariant(),title=status==500?"Lỗi hệ thống":ex is BusinessException?ex.Message:"Dữ liệu xung đột, hãy tải lại hoặc gửi lại cùng mã yêu cầu.",status,code,traceId=ctx.TraceIdentifier},options:(System.Text.Json.JsonSerializerOptions?)null,contentType:"application/problem+json");}
 }
});
// Log & error — chuẩn hóa cả 401/403/404/429 khi middleware không có body.
app.UseStatusCodePages(async context=>{
 var response=context.HttpContext.Response;
 await response.WriteAsJsonAsync(new{title=response.StatusCode switch{401=>"Vui lòng đăng nhập.",403=>"Bạn không có quyền thực hiện chức năng này.",404=>"Không tìm thấy tài nguyên.",429=>"Quá nhiều yêu cầu, vui lòng thử lại sau.",_=>"Yêu cầu không thể xử lý."},status=response.StatusCode,traceId=context.HttpContext.TraceIdentifier});
});

app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.Run();
public partial class Program { }
