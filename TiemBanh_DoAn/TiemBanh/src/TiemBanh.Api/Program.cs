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
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.Run();
public partial class Program { }
