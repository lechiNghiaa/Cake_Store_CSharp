using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiemBanh.DataAccess;
namespace TiemBanh.Business;
// ==================== UC01–UC03 / UC12 / UC15 — TÀI KHOẢN ====================
public class AccountService(BakeryStore s) {
 readonly PasswordHasher<AppUser> hasher=new();
 public static UserView View(AppUser u)=>new(u.Id,u.UserName,u.FullName,u.Phone,u.Address,u.Role,u.Active,u.Version);
 // Đăng nhập — kiểm tra mật khẩu và tài khoản bị khóa.
 public async Task<AppUser> LoginAsync(LoginRequest r) {
  var name=r.UserName.Trim().ToLowerInvariant();var u=await s.Query<AppUser>().SingleOrDefaultAsync(x=>x.UserName==name);
  Rules.Check(u!=null&&u.Active&&hasher.VerifyHashedPassword(u,u.PasswordHash,r.Password)!=PasswordVerificationResult.Failed,"LOGIN_FAILED","Tên đăng nhập hoặc mật khẩu không hợp lệ.",401);return u!;
 }
 // Đăng ký — chỉ tạo vai trò Khách hàng.
 public Task<UserView> RegisterAsync(RegisterRequest r)=>CreateAsync(r.UserName,r.Password,r.FullName,r.Phone,r.Address,"Customer");
 async Task<UserView> CreateAsync(string name,string password,string fullName,string phone,string address,string role)=>await s.WriteAsync(async()=>{
  name=name.Trim().ToLowerInvariant();Rules.Check(name.Length>=3&&!await s.Query<AppUser>().AnyAsync(x=>x.UserName==name),"USERNAME_EXISTS","Tên đăng nhập đã tồn tại hoặc không hợp lệ.");
  var u=new AppUser{UserName=name,FullName=fullName.Trim(),Phone=phone.Trim(),Address=address.Trim(),Role=role};u.PasswordHash=hasher.HashPassword(u,password);s.Add(u);await s.SaveAsync();return View(u);
 });
 public async Task<UserView> MeAsync(Actor a)=>View(await s.Query<AppUser>().SingleAsync(x=>x.Id==a.Id));
 // Hồ sơ cá nhân — chỉ cập nhật người dùng hiện tại.
 public Task<UserView> ProfileAsync(Actor a,ProfileRequest r)=>s.WriteAsync(async()=>{var u=await s.Query<AppUser>().SingleAsync(x=>x.Id==a.Id);u.FullName=r.FullName.Trim();u.Phone=r.Phone.Trim();u.Address=r.Address.Trim();u.Version++;return View(u);});
 // Quản lý nhân viên — Admin tạo Nhân viên / Người giao hàng.
 public Task<UserView> StaffAsync(Actor a,StaffRequest r) {Rules.Role(a,"Admin");Rules.Check(r.Role is "Employee" or "Delivery","INVALID_ROLE","Chọn Nhân viên hoặc Người giao hàng.",400);return CreateAsync(r.UserName,r.Password,r.FullName,r.Phone,"",r.Role);}
 public Task<UserView> UpdateStaffAsync(Actor a,int id,StaffUpdateRequest r) {Rules.Role(a,"Admin");return s.WriteAsync(async()=>{
  var u=await s.Query<AppUser>().SingleOrDefaultAsync(x=>x.Id==id&&(x.Role=="Employee"||x.Role=="Delivery"))??throw new BusinessException("NOT_FOUND","Không có nhân viên.",404);
  Rules.Version(u.Version,r.Version);Rules.Check(r.Role is "Employee" or "Delivery","INVALID_ROLE","Vai trò không hợp lệ.",400);
  Rules.Check(u.Role==r.Role||!await s.Query<DeliveryAttempt>().AnyAsync(x=>x.DriverId==id&&x.Result=="Assigned"),"DRIVER_BUSY","Không đổi vai trò khi đang giao hàng.");
  u.FullName=r.FullName;u.Phone=r.Phone;u.Role=r.Role;u.Version++;u.SecurityStamp=Guid.NewGuid().ToString();return View(u);
 });}
 // Quản lý khách hàng — Employee tra cứu, Admin khóa/mở.
 public async Task<List<UserView>> UsersAsync(Actor a,string kind,string? search) {
  Rules.Role(a,"Employee","Admin");Rules.Check(kind is "customers" or "staff","INVALID_KIND","Nhóm tài khoản không hợp lệ.",400);if(kind=="staff")Rules.Role(a,"Admin");
  var q=s.Query<AppUser>().Where(x=>kind=="customers"?x.Role=="Customer":x.Role=="Employee"||x.Role=="Delivery");
  if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.FullName.Contains(search)||x.UserName.Contains(search)||x.Phone.Contains(search));
  return (await q.OrderBy(x=>x.Id).Take(200).ToListAsync()).Select(View).ToList();
 }
 public Task<UserView> ActiveAsync(Actor a,int id,ActiveRequest r) {Rules.Role(a,"Admin");return s.WriteAsync(async()=>{
  var u=await s.Query<AppUser>().SingleOrDefaultAsync(x=>x.Id==id&&x.Role!="Guest")??throw new BusinessException("NOT_FOUND","Không có tài khoản.",404);
  Rules.Check(u.Role!="Admin","ADMIN_PROTECTED","Không khóa tài khoản Admin qua chức năng này.");Rules.Version(u.Version,r.Version);u.Active=r.Active;u.SecurityStamp=Guid.NewGuid().ToString();u.Version++;return View(u);
 });}
 public async Task<List<UserView>> DriversAsync(Actor a){Rules.Role(a,"Employee");return (await s.Query<AppUser>().Where(x=>x.Role=="Delivery"&&x.Active).ToListAsync()).Select(View).ToList();}
}
