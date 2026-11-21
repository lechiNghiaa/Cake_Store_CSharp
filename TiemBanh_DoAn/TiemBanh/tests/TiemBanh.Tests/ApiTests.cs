
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TiemBanh.DataAccess;
using Xunit;
namespace TiemBanh.Tests;
// Kiểm thử tích hợp HTTP — SQLite chỉ là adapter test, production vẫn SQL Server.
// Không dùng kết quả này thay thế kiểm thử isolation/deadlock trên SQL Server.
public sealed class BakeryFactory:WebApplicationFactory<Program> {
 readonly SqliteConnection connection=new("Data Source=:memory:");
 public BakeryFactory(){connection.Open();Environment.SetEnvironmentVariable("Jwt__SigningKey","TestingOnly-Key-32-Bytes-Long-DoNotUseInProduction");Environment.SetEnvironmentVariable("ConnectionStrings__Bakery","Server=unused;Database=unused");Environment.SetEnvironmentVariable("Uploads__Root",Path.Combine(Path.GetTempPath(),"tiembanh-test-uploads"));}
 protected override void ConfigureWebHost(IWebHostBuilder builder){builder.UseEnvironment("Testing");builder.ConfigureServices(services=>{
  services.RemoveAll<BakeryDbContext>();services.RemoveAll<DbContextOptions<BakeryDbContext>>();services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<BakeryDbContext>>();
  services.AddDbContext<BakeryDbContext>(o=>o.UseSqlite(connection));
 });}
 public async Task Seed(){using var scope=Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<BakeryDbContext>();await db.Database.EnsureCreatedAsync();
  var hasher=new PasswordHasher<AppUser>();foreach(var(name,role)in new[]{("admin","Admin"),("employee","Employee"),("driver","Delivery"),("otherdriver","Delivery"),("customer","Customer"),("othercustomer","Customer")}){var u=new AppUser{UserName=name,FullName=name,Role=role};u.PasswordHash=hasher.HashPassword(u,"TestingPassword9!");db.Add(u);}
  db.Add(new Category{Name="Bánh test"});await db.SaveChangesAsync();
 }
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)connection.Dispose();}
}
public class ApiTests {
 [Fact] public async Task GuestCheckout_ReservesOnce_AndKeepsOrderListPrivate(){
  using var factory=new BakeryFactory();
  await factory.Seed();
  using var scope=factory.Services.CreateScope();
  var db=scope.ServiceProvider.GetRequiredService<BakeryDbContext>();
  var cake=new Cake{Code="GUEST",Name="Bánh thử",CategoryId=1,Price=35000,Inventory=new Inventory{OnHand=3}};
  db.Add(cake);
  await db.SaveChangesAsync();
  using var guest=factory.CreateClient();
  var request=new {requestId=Guid.NewGuid(),lines=new[]{new {cakeId=cake.Id,quantity=2}},recipient="Khách thử",phone="0900000000",address="Địa chỉ thử"};
  var first=await guest.PostAsJsonAsync("/api/orders/guest",request);
  Assert.Equal(HttpStatusCode.Created,first.StatusCode);
  var receipt=await first.Content.ReadFromJsonAsync<JsonElement>();
  Assert.Equal(90000m,receipt.GetProperty("total").GetDecimal());
  var retry=await guest.PostAsJsonAsync("/api/orders/guest",request);
  retry.EnsureSuccessStatusCode();
  Assert.Equal(receipt.GetProperty("code").GetString(),(await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
  Assert.Equal(HttpStatusCode.Unauthorized,(await guest.GetAsync("/api/orders")).StatusCode);
  Assert.Equal(1,await db.Set<Order>().CountAsync());
  Assert.Equal(2,await db.Set<Inventory>().SumAsync(i=>i.Reserved));
 }
 // Một ca xuyên suốt có kiểm chứng từng giai đoạn để giữ dữ liệu nhất quán.
 [Fact] public async Task HTTP_LuongCOD_VaPhanQuyen(){
  using var f=new BakeryFactory();await f.Seed();using var anon=f.CreateClient();
  async Task<HttpClient> Login(string name){var c=f.CreateClient();var r=await c.PostAsJsonAsync("/api/auth/login",new{userName=name,password="TestingPassword9!"});r.EnsureSuccessStatusCode();var j=await r.Content.ReadFromJsonAsync<JsonElement>();c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",j.GetProperty("token").GetString());return c;}
  async Task<JsonElement> Json(HttpResponseMessage r){Assert.True(r.IsSuccessStatusCode,await r.Content.ReadAsStringAsync());return await r.Content.ReadFromJsonAsync<JsonElement>();}
  using var admin=await Login("admin");using var e=await Login("employee");using var d=await Login("driver");using var d2=await Login("otherdriver");using var c=await Login("customer");using var c2=await Login("othercustomer");
  // UC01 — Đăng nhập sai; UC02 — đăng ký công khai không nhận quyền Admin.
  Assert.Equal(HttpStatusCode.Unauthorized,(await anon.PostAsJsonAsync("/api/auth/login",new{userName="customer",password="wrong"})).StatusCode);
  var reg=await Json(await anon.PostAsJsonAsync("/api/auth/register",new{userName="newcustomer",password="PasswordForTest9!",fullName="Khách mới",role="Admin"}));Assert.Equal("Customer",reg.GetProperty("role").GetString());
  Assert.Equal(HttpStatusCode.Conflict,(await anon.PostAsJsonAsync("/api/auth/register",new{userName="newcustomer",password="PasswordForTest9!",fullName="Trùng"})).StatusCode);
  // UC03 — Hồ sơ cá nhân.
  var profile=await Json(await c.PutAsJsonAsync("/api/auth/me",new{fullName="Khách kiểm thử",phone="0900000000",address="Địa chỉ test"}));Assert.Equal("Khách kiểm thử",profile.GetProperty("fullName").GetString());
  // UC10 — Thêm bánh, Điều chỉnh tồn và phân quyền Admin.
  var cake=await Json(await admin.PostAsJsonAsync("/api/cakes",new{code="TEST01",name="Bánh kiểm thử",categoryId=1,price=50000m}));var cakeId=cake.GetProperty("id").GetInt32();
  var list=await Json(await admin.GetAsync("/api/cakes/manage"));var item=list.GetProperty("items")[0];
  await Json(await admin.PutAsJsonAsync($"/api/cakes/{cakeId}/stock",new{onHand=10,version=item.GetProperty("stockVersion").GetInt64(),reason="Kiểm thử"}));
  Assert.Equal(HttpStatusCode.Forbidden,(await e.PutAsJsonAsync($"/api/cakes/{cakeId}/stock",new{onHand=5,version=1,reason="Sai quyền"})).StatusCode);
  // UC04–UC05 — Danh sách bánh, Tìm kiếm bánh.
  var catalog=await Json(await anon.GetAsync("/api/cakes?search=kiểm&categoryId=1"));Assert.Equal(1,catalog.GetProperty("total").GetInt32());
  Assert.Equal(0,(await Json(await anon.GetAsync("/api/cakes?search=khongtontai"))).GetProperty("total").GetInt32());
  // UC07 — Tính tổng COD, snapshot giá và chống gửi lặp.
  var request=new{requestId=Guid.NewGuid(),lines=new[]{new{cakeId,quantity=2}},recipient="Người nhận",phone="0900000000",address="Địa chỉ",note=""};
  var order=await Json(await c.PostAsJsonAsync("/api/orders",request));var id=order.GetProperty("id").GetInt32();Assert.Equal(120000m,order.GetProperty("total").GetDecimal());
  var repeat=await Json(await c.PostAsJsonAsync("/api/orders",request));Assert.Equal(id,repeat.GetProperty("id").GetInt32());
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/orders",new{request.requestId,lines=new[]{new{cakeId,quantity=3}},request.recipient,request.phone,request.address,note=""})).StatusCode);
  var inventory=(await Json(await admin.GetAsync("/api/cakes/manage"))).GetProperty("items")[0];Assert.Equal(2,inventory.GetProperty("reserved").GetInt32());
  // UC08 — người khác không thấy/hủy đơn; Version cũ bị chặn.
  Assert.Equal(0,(await Json(await c2.GetAsync("/api/orders"))).GetArrayLength());
  Assert.Equal(HttpStatusCode.NotFound,(await c2.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="cancel",version=1})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="cancel",version=999})).StatusCode);
  // UC14 — Employee xử lý; Admin không được xác nhận thay.
  Assert.Equal(HttpStatusCode.Forbidden,(await admin.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="confirm",version=1})).StatusCode);
  order=await Json(await e.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="confirm",version=1}));
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="cancel",version=2})).StatusCode);
  order=await Json(await e.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="prepare",version=2}));
  order=await Json(await e.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="assign",version=3,driverId=3}));var attempt=order.GetProperty("deliveries")[0].GetProperty("id").GetInt32();
  // UC09 — đúng người giao, đủ tiền, một phiếu thu.
  Assert.Equal(HttpStatusCode.Forbidden,(await d2.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="complete",version=4,deliveryAttemptId=attempt,collectedAmount=120000})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await d.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="complete",version=4,deliveryAttemptId=attempt,collectedAmount=119000})).StatusCode);
  order=await Json(await d.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="complete",version=4,deliveryAttemptId=attempt,collectedAmount=120000}));Assert.Equal("Completed",order.GetProperty("status").GetString());
  Assert.Equal(HttpStatusCode.Conflict,(await d.PostAsJsonAsync($"/api/orders/{id}/actions",new{action="complete",version=5,deliveryAttemptId=attempt,collectedAmount=120000})).StatusCode);
  inventory=(await Json(await admin.GetAsync("/api/cakes/manage"))).GetProperty("items")[0];Assert.Equal(8,inventory.GetProperty("onHand").GetInt32());Assert.Equal(0,inventory.GetProperty("reserved").GetInt32());
  // UC11 — lập kỳ, chặn lệch, sửa kỳ, duyệt và khóa.
  var receipts=await Json(await e.GetAsync("/api/management/receipts"));Assert.Equal(1,receipts.GetArrayLength());var receiptId=receipts[0].GetProperty("id").GetInt32();
  var period=await Json(await e.PostAsJsonAsync("/api/management/periods",new{items=new[]{new{receiptId,received=119000,note="Thiếu tạm thời"}}}));var pid=period.GetProperty("id").GetInt32();var pv=period.GetProperty("version").GetInt64();
  Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync($"/api/management/periods/{pid}/approve",new{version=pv})).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await e.PostAsJsonAsync($"/api/management/periods/{pid}/approve",new{version=pv})).StatusCode);
  period=await Json(await e.PutAsJsonAsync($"/api/management/periods/{pid}",new{version=pv,items=new[]{new{receiptId,received=120000,note="Đã bổ sung"}}}));
  period=await Json(await admin.PostAsJsonAsync($"/api/management/periods/{pid}/approve",new{version=period.GetProperty("version").GetInt64()}));
  period=await Json(await admin.PostAsJsonAsync($"/api/management/periods/{pid}/lock",new{version=period.GetProperty("version").GetInt64()}));Assert.Equal("Locked",period.GetProperty("status").GetString());
  Assert.Equal(HttpStatusCode.Conflict,(await e.PutAsJsonAsync($"/api/management/periods/{pid}",new{version=period.GetProperty("version").GetInt64(),items=new[]{new{receiptId,received=120000,note="Sửa sau khóa"}}})).StatusCode);
  // UC13 — doanh thu thực thu; Employee không được xem báo cáo Admin.
  var report=await Json(await admin.GetAsync("/api/management/report"));Assert.Equal(120000m,report.GetProperty("revenue").GetDecimal());Assert.Equal(HttpStatusCode.Forbidden,(await e.GetAsync("/api/management/report")).StatusCode);
  // UC12, UC15 — tạo/sửa staff, khóa khách và thu hồi token.
  var staff=await Json(await admin.PostAsJsonAsync("/api/management/staff",new{userName="newstaff",password="PasswordForTest9!",fullName="Nhân viên mới",role="Employee"}));
  await Json(await admin.PutAsJsonAsync("/api/management/staff/"+staff.GetProperty("id").GetInt32(),new{fullName="Đã sửa",phone="0900",role="Delivery",version=staff.GetProperty("version").GetInt64()}));
  Assert.Equal(HttpStatusCode.Forbidden,(await e.GetAsync("/api/management/users/staff")).StatusCode);
  var customers=await Json(await e.GetAsync("/api/management/users/customers?search=othercustomer"));var other=customers[0];var uid=other.GetProperty("id").GetInt32();
  Assert.Equal(HttpStatusCode.Forbidden,(await e.PutAsJsonAsync($"/api/management/users/{uid}/active",new{active=false,version=other.GetProperty("version").GetInt64()})).StatusCode);
  await Json(await admin.PutAsJsonAsync($"/api/management/users/{uid}/active",new{active=false,version=other.GetProperty("version").GetInt64()}));Assert.Equal(HttpStatusCode.Unauthorized,(await c2.GetAsync("/api/auth/me")).StatusCode);
  // UC08 — Hủy Pending giải phóng đúng lượng giữ.
  var cancel=await Json(await c.PostAsJsonAsync("/api/orders",new{requestId=Guid.NewGuid(),lines=new[]{new{cakeId,quantity=1}},recipient="Test",phone="0900",address="Test"}));
  await Json(await c.PostAsJsonAsync("/api/orders/"+cancel.GetProperty("id").GetInt32()+"/actions",new{action="cancel",version=1}));
  inventory=(await Json(await admin.GetAsync("/api/cakes/manage"))).GetProperty("items")[0];Assert.Equal(0,inventory.GetProperty("reserved").GetInt32());
  // Giao thất bại / Kiểm bánh hoàn — hỏng một bánh, giải phóng hai bánh.
  var failed=await Json(await c.PostAsJsonAsync("/api/orders",new{requestId=Guid.NewGuid(),lines=new[]{new{cakeId,quantity=2}},recipient="Test",phone="0900",address="Test"}));var fid=failed.GetProperty("id").GetInt32();
  await Json(await e.PostAsJsonAsync($"/api/orders/{fid}/actions",new{action="confirm",version=1}));await Json(await e.PostAsJsonAsync($"/api/orders/{fid}/actions",new{action="prepare",version=2}));failed=await Json(await e.PostAsJsonAsync($"/api/orders/{fid}/actions",new{action="assign",version=3,driverId=3}));
  await Json(await d.PostAsJsonAsync($"/api/orders/{fid}/actions",new{action="fail",version=4,deliveryAttemptId=failed.GetProperty("deliveries")[0].GetProperty("id").GetInt32(),reason="Không liên hệ được"}));
  await Json(await e.PostAsJsonAsync($"/api/orders/{fid}/actions",new{action="close-failed",version=5,reason="Một bánh hỏng",damagedLines=new[]{new{cakeId,quantity=1}}}));
  inventory=(await Json(await admin.GetAsync("/api/cakes/manage"))).GetProperty("items")[0];Assert.Equal(7,inventory.GetProperty("onHand").GetInt32());Assert.Equal(0,inventory.GetProperty("reserved").GetInt32());
 }
 // Đặt hàng nhiều dòng — lỗi dòng sau phải rollback giữ tồn và toàn bộ đơn.
 [Fact] public async Task HTTP_DatNhieuDong_ThieuTon_RollbackTatCa(){
  using var f=new BakeryFactory();await f.Seed();
  async Task<HttpClient> Login(string name){var c=f.CreateClient();var result=await c.PostAsJsonAsync("/api/auth/login",new{userName=name,password="TestingPassword9!"});result.EnsureSuccessStatusCode();var data=await result.Content.ReadFromJsonAsync<JsonElement>();c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",data.GetProperty("token").GetString());return c;}
  using var admin=await Login("admin");using var customer=await Login("customer");
  var ids=new List<int>();
  foreach(var code in new[]{"ROLLBACK1","ROLLBACK2"}){var r=await admin.PostAsJsonAsync("/api/cakes",new{code,name=code,categoryId=1,price=50000});r.EnsureSuccessStatusCode();ids.Add((await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());}
  var stocks=await (await admin.GetAsync("/api/cakes/manage")).Content.ReadFromJsonAsync<JsonElement>();var first=stocks.GetProperty("items")[0];
  (await admin.PutAsJsonAsync($"/api/cakes/{ids[0]}/stock",new{onHand=5,version=first.GetProperty("stockVersion").GetInt64(),reason="Rollback test"})).EnsureSuccessStatusCode();
  var result=await customer.PostAsJsonAsync("/api/orders",new{requestId=Guid.NewGuid(),lines=new[]{new{cakeId=ids[0],quantity=2},new{cakeId=ids[1],quantity=1}},recipient="Test",phone="0900",address="Test"});
  Assert.Equal(HttpStatusCode.Conflict,result.StatusCode);
  using var scope=f.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<BakeryDbContext>();
  Assert.Equal(0,await db.Set<Order>().CountAsync());Assert.Equal(0,await db.Set<Inventory>().SumAsync(x=>x.Reserved));Assert.Equal(0,await db.Set<StockMovement>().CountAsync(x=>x.Reason.StartsWith("Đặt hàng")));
 }

}
