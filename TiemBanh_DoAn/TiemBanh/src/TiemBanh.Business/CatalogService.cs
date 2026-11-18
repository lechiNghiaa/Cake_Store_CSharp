using Microsoft.EntityFrameworkCore;
using TiemBanh.DataAccess;
namespace TiemBanh.Business;
// ==================== UC04–UC05 / UC10 — BÁNH VÀ TỒN KHO ====================
public class CatalogService(BakeryStore s) {
 // Danh sách bánh / Tìm kiếm bánh — lọc tên, mã và danh mục; phân trang.
 public async Task<object> ListAsync(string? search,int? categoryId,int page=1,int pageSize=12,bool manage=false) {
  var q=s.Query<Cake>().Include(x=>x.Inventory).AsNoTracking().Where(x=>manage||x.Active);
  if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Name.Contains(search)||x.Code.Contains(search));
  if(categoryId.HasValue)q=q.Where(x=>x.CategoryId==categoryId);page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);
  var total=await q.CountAsync();var items=await q.OrderBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.Code,x.Name,x.CategoryId,x.Price,x.Description,x.Image,x.Active,x.Version,x.Inventory.OnHand,x.Inventory.Reserved,x.Inventory.Available,StockVersion=x.Inventory.Version}).ToListAsync();
  return new{items,total,page,pageSize};
 }
 public async Task<object> CategoriesAsync()=>await s.Query<Category>().OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name}).ToListAsync();
 // Quản lý bánh — cập nhật có Version, không xóa lịch sử.
 public Task<object> SaveAsync(Actor a,int? id,CakeRequest r) {Rules.Role(a,"Admin");return s.WriteAsync<object>(async()=>{
  Rules.Check(await s.Query<Category>().AnyAsync(x=>x.Id==r.CategoryId),"CATEGORY_INVALID","Danh mục không tồn tại.",400);
  Rules.Check(!await s.Query<Cake>().AnyAsync(x=>x.Code==r.Code&&x.Id!=id),"CODE_EXISTS","Mã bánh đã tồn tại.");
  Rules.Check(string.IsNullOrEmpty(r.Image)||System.Text.RegularExpressions.Regex.IsMatch(r.Image,@"^/uploads/[a-f0-9]{32}\.(jpg|png|webp)$"),"IMAGE_INVALID","Đường dẫn ảnh không hợp lệ.",400);
  var c=id.HasValue?await s.Query<Cake>().Include(x=>x.Inventory).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("NOT_FOUND","Không có bánh.",404):new Cake{Inventory=new Inventory()};
  if(id.HasValue)Rules.Version(c.Version,r.Version);else s.Add(c);
  c.Code=r.Code.Trim();c.Name=r.Name.Trim();c.CategoryId=r.CategoryId;c.Price=r.Price;c.Description=r.Description;c.Image=r.Image;c.Active=r.Active;c.Version++;await s.SaveAsync();return new{c.Id,c.Version};
 });}
 // Điều chỉnh tồn — không giảm OnHand thấp hơn lượng đã giữ.
 public Task<object> StockAsync(Actor a,int id,StockRequest r){Rules.Role(a,"Admin");return s.WriteAsync<object>(async()=>{
  var x=await s.Query<Inventory>().SingleOrDefaultAsync(x=>x.CakeId==id)??throw new BusinessException("NOT_FOUND","Không có tồn kho.",404);Rules.Version(x.Version,r.Version);Rules.Stock(r.OnHand,x.Reserved);
  s.Add(new StockMovement{CakeId=id,ActorId=a.Id,OnHandDelta=r.OnHand-x.OnHand,Reason=r.Reason});x.OnHand=r.OnHand;x.Version++;return new{x.OnHand,x.Reserved,Available=x.OnHand-x.Reserved,x.Version};
 });}
}
