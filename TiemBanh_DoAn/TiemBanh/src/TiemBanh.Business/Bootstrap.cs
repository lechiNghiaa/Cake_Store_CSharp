using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiemBanh.DataAccess;
namespace TiemBanh.Business;
// ==================== KHỞI TẠO BA LỚP — composition root tập trung ====================
public static class Bootstrap {
 public static IServiceCollection AddBakery(this IServiceCollection services,IConfiguration config) {
  services.AddDbContext<BakeryDbContext>(o=>o.UseSqlServer(config.GetConnectionString("Bakery")??throw new InvalidOperationException("Thiếu ConnectionStrings__Bakery.")));
  services.AddScoped<BakeryStore>();services.AddScoped<AccountService>();services.AddScoped<CatalogService>();services.AddScoped<OrderService>();services.AddScoped<CodService>();return services;
 }
 // Migration và seed — không chứa mật khẩu thật trong source hoặc image.
 public static async Task MigrateAsync(IServiceProvider provider,IConfiguration cfg) {
  var db=provider.GetRequiredService<BakeryDbContext>();await db.Database.MigrateAsync();
  if(await db.Set<AppUser>().AnyAsync())return;
  var password=cfg["Seed:AdminPassword"]??throw new InvalidOperationException("Thiếu Seed__AdminPassword cho lần khởi tạo đầu.");
  if(password.Length<12)throw new InvalidOperationException("Mật khẩu seed phải ít nhất 12 ký tự.");
  var hasher=new PasswordHasher<AppUser>();var roles=cfg.GetValue<bool>("Seed:Demo")?new[]{("admin","Admin"),("nhanvien","Employee"),("giaohang","Delivery"),("khachhang","Customer")}:new[]{("admin","Admin")};
  foreach(var(name,role)in roles){var u=new AppUser{UserName=name,Role=role,FullName=role switch{"Admin"=>"Quản trị viên","Employee"=>"Nhân viên tiệm bánh","Delivery"=>"Người giao hàng",_=>"Khách hàng mẫu"}};u.PasswordHash=hasher.HashPassword(u,password);db.Add(u);}
  var cakes=new[]{("Bánh ngọt","Bánh dâu kem tươi",65000m,"Strawberry cream cake"),("Bánh mì","Croissant bơ Pháp",35000m,"Croissant"),("Bánh quy","Cookie chocolate",25000m,"Chocolate cookie"),("Bánh ngọt","Tiramisu cacao",75000m,"Tiramisu"),("Bánh mì","Bánh mì hạt",42000m,"Seed bread"),("Bánh quy","Bánh quy bơ",30000m,"Butter cookie")};
  var categories=cakes.Select(x=>x.Item1).Distinct().ToDictionary(x=>x,x=>new Category{Name=x});db.AddRange(categories.Values);
  var i=0;foreach(var(cat,name,price,description)in cakes){i++;db.Add(new Cake{Code="BANH"+i.ToString("000"),Name=name,Category=categories[cat],Price=price,Description=description,Inventory=new Inventory{OnHand=30}});}
  await db.SaveChangesAsync();
 }
}
