using Microsoft.EntityFrameworkCore;
using TiemBanh.DataAccess;
namespace TiemBanh.Business;
// ==================== UC11 / UC13 — ĐỐI SOÁT COD VÀ BÁO CÁO ====================
public class CodService(BakeryStore s) {
 public async Task<object> ReceiptsAsync(Actor a){Rules.Role(a,"Employee","Admin");return await s.Query<CodReceipt>().Where(x=>!s.Query<CodHandover>().Any(h=>h.ReceiptId==x.Id)).Select(x=>new{x.Id,x.OrderId,x.Amount,x.CollectedAt,x.DriverId}).ToListAsync();}
 public async Task<object> PeriodsAsync(Actor a){Rules.Role(a,"Employee","Admin");return (await s.Query<CodPeriod>().Include(x=>x.Items).OrderByDescending(x=>x.Id).Take(100).ToListAsync()).Select(View).ToList();}
 static object View(CodPeriod p)=>new{p.Id,p.Date,p.Status,p.Version,p.CreatedById,p.ApprovedById,p.ApprovedAt,p.LockedAt,Expected=p.Items.Sum(x=>x.Expected),Received=p.Items.Sum(x=>x.Received),Items=p.Items.Select(x=>new{x.ReceiptId,x.Expected,x.Received,x.Note})};
 // Lập kỳ bàn giao COD — Expected lấy từ phiếu thu, không tin giá trị phía client.
 public Task<object> SavePeriodAsync(Actor a,int? id,PeriodRequest r){Rules.Role(a,"Employee");return s.WriteAsync<object>(async()=>{
  Rules.Check(r.Items.Count>0&&r.Items.Select(x=>x.ReceiptId).Distinct().Count()==r.Items.Count,"EMPTY_PERIOD","Kỳ cần có các phiếu thu khác nhau.",400);
  var p=id.HasValue?await s.Query<CodPeriod>().Include(x=>x.Items).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("NOT_FOUND","Không tìm thấy kỳ.",404):new CodPeriod{CreatedById=a.Id};
  if(id.HasValue){Rules.Version(p.Version,r.Version);Rules.Check(p.Status=="Submitted","PERIOD_LOCKED","Chỉ sửa kỳ chưa duyệt.");Rules.Check(p.Items.Select(x=>x.ReceiptId).Order().SequenceEqual(r.Items.Select(x=>x.ReceiptId).Order()),"RECEIPTS_FIXED","Khi sửa kỳ chỉ điều chỉnh tiền thực nhận và ghi chú.",400);}
  else s.Add(p);
  foreach(var item in r.Items){
   var receipt=await s.Query<CodReceipt>().SingleOrDefaultAsync(x=>x.Id==item.ReceiptId)??throw new BusinessException("RECEIPT_INVALID","Phiếu thu không hợp lệ.",400);
   Rules.Check(!await s.Query<CodHandover>().AnyAsync(x=>x.ReceiptId==item.ReceiptId&&(!id.HasValue||x.PeriodId!=id)),"RECEIPT_REUSED","Phiếu thu đã thuộc kỳ khác.");
   Rules.Check(item.Received>=0&&(item.Received==receipt.Amount||!string.IsNullOrWhiteSpace(item.Note)),"NOTE_REQUIRED","Chênh lệch phải có ghi chú.",400);
   if(id.HasValue){var h=p.Items.Single(x=>x.ReceiptId==item.ReceiptId);h.Received=item.Received;h.Note=item.Note;}
   else p.Items.Add(new CodHandover{ReceiptId=receipt.Id,Expected=receipt.Amount,Received=item.Received,Note=item.Note});
  }
  p.Version++;await s.SaveAsync();return View(p);
 });}
 // Duyệt kỳ COD / Khóa kỳ COD — phân tách quyền với bước lập kỳ.
 public Task<object> ActionAsync(Actor a,int id,string action,long version){Rules.Role(a,"Admin");return s.WriteAsync<object>(async()=>{
  var p=await s.Query<CodPeriod>().Include(x=>x.Items).SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("NOT_FOUND","Không tìm thấy kỳ.",404);Rules.Version(p.Version,version);
  if(action=="approve"){Rules.Check(p.Status=="Submitted"&&p.Items.Count>0&&p.Items.All(x=>x.Expected==x.Received),"PERIOD_MISMATCH","Kỳ phải chưa duyệt, có dữ liệu và từng khoản khớp.");p.Status="Approved";p.ApprovedById=a.Id;p.ApprovedAt=DateTime.UtcNow;}
  else {Rules.Check(action=="lock"&&p.Status=="Approved","INVALID_PERIOD_STATE","Chỉ khóa kỳ đã duyệt.");p.Status="Locked";p.LockedAt=DateTime.UtcNow;}
  p.Version++;return View(p);
 });}
 // Báo cáo thống kê — chỉ cộng doanh thu đơn hoàn thành đã thu đủ COD.
 public async Task<object> ReportAsync(Actor a){Rules.Role(a,"Admin");var revenue=await s.Query<CodReceipt>().Where(x=>x.Order.Status=="Completed"&&x.Amount==x.Order.Total).SumAsync(x=>(decimal?)x.Amount)??0;
  return new{Revenue=revenue,OrderCounts=await s.Query<Order>().GroupBy(x=>x.Status).Select(g=>new{Status=g.Key,Count=g.Count()}).ToListAsync(),Available=await s.Query<Inventory>().SumAsync(x=>(int?)x.Available)??0};
 }
}
