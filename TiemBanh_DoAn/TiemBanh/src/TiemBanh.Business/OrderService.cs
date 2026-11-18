using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TiemBanh.DataAccess;
namespace TiemBanh.Business;
// ==================== UC07–UC09 / UC14 — ĐƠN HÀNG VÀ GIAO COD ====================
public class OrderService(BakeryStore s) {
 IQueryable<Order> Full()=>s.Query<Order>().Include(x=>x.Lines).Include(x=>x.History).Include(x=>x.Deliveries);
 public async Task<object> ListAsync(Actor a) {
  Rules.Role(a,"Customer","Employee","Delivery");var q=Full().AsNoTracking();
  if(a.Role=="Customer")q=q.Where(x=>x.CustomerId==a.Id);
  if(a.Role=="Delivery")q=q.Where(x=>x.Deliveries.Any(d=>d.DriverId==a.Id&&d.Result=="Assigned"));
  return (await q.OrderByDescending(x=>x.Id).Take(100).ToListAsync()).Select(View).ToList();
 }
 public static object View(Order o)=>new{o.Id,o.Code,o.CustomerId,o.Recipient,o.Phone,o.Address,o.Note,o.Total,o.Shipping,o.Status,o.CreatedAt,o.Version,o.RequestId,Lines=o.Lines.Select(x=>new{x.CakeId,x.CakeName,x.UnitPrice,x.Quantity}),History=o.History.OrderBy(x=>x.Id).Select(x=>new{x.From,x.To,x.Reason,x.At,x.ActorId}),Deliveries=o.Deliveries.OrderBy(x=>x.Sequence).Select(x=>new{x.Id,x.DriverId,x.Sequence,x.Result,x.Reason})};
 // Đặt hàng COD — chuẩn hóa giỏ, chống gửi lặp và tự tính giá phía máy chủ.
 public Task<object> PlaceAsync(Actor? customer,CreateOrderRequest r) {
  if(customer!=null) Rules.Role(customer,"Customer");
  return s.WriteAsync<object>(async()=>{
  Rules.Check(r.RequestId!=Guid.Empty&&r.Lines.Count>0&&r.Lines.All(x=>x.Quantity>0&&x.Quantity<=10000),"INVALID_CART","Giỏ hàng không hợp lệ.",400);
  var lines=r.Lines.GroupBy(x=>x.CakeId).Select(g=>new LineRequest(g.Key,g.Sum(x=>x.Quantity))).OrderBy(x=>x.CakeId).ToList();
  var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{lines,r.Recipient,r.Phone,r.Address,r.Note}))));
  var customerId=customer?.Id ?? await GuestActorIdAsync();
  var old=await Full().SingleOrDefaultAsync(x=>x.CustomerId==customerId&&x.RequestId==r.RequestId);
  if(old!=null){Rules.Check(old.RequestHash==hash,"REQUEST_REUSED","Mã yêu cầu đã dùng cho nội dung khác.");return Receipt(old);}
  var o=new Order{CustomerId=customerId,RequestId=r.RequestId,RequestHash=hash,Code="TB"+Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),Recipient=r.Recipient.Trim(),Phone=r.Phone.Trim(),Address=r.Address.Trim(),Note=r.Note,Shipping=Rules.Shipping};
  foreach(var line in lines) {
   var c=await s.Query<Cake>().Include(x=>x.Inventory).SingleOrDefaultAsync(x=>x.Id==line.CakeId&&x.Active)??throw new BusinessException("CAKE_UNAVAILABLE","Bánh không còn được bán.");
   var stock=c.Inventory;Rules.Stock(stock.OnHand,checked(stock.Reserved+line.Quantity));stock.Reserved+=line.Quantity;stock.Version++;
   o.Lines.Add(new OrderLine{CakeId=c.Id,CakeName=c.Name,UnitPrice=c.Price,Quantity=line.Quantity});
   s.Add(new StockMovement{CakeId=c.Id,ActorId=customerId,ReservedDelta=line.Quantity,Reason="Đặt hàng COD: "+o.Code});
  }
  // Tính tổng COD — tiền bánh theo snapshot + phí giao hàng.
  o.Total=Rules.Total(o.Lines.Select(x=>(x.UnitPrice,x.Quantity)),o.Shipping);o.History.Add(new OrderHistory{ActorId=customerId,To="Pending",Reason="Đặt hàng COD"});s.Add(o);await s.SaveAsync();return Receipt(o);
  });
  object Receipt(Order order)=>customer==null ? new {order.Code,order.Total} : View(order);
 }
 // Định danh nội bộ cho lịch sử xử lý và giữ tồn của đơn vãng lai.
 async Task<int> GuestActorIdAsync(){
  var guest=await s.Query<AppUser>().SingleOrDefaultAsync(u=>u.Role=="Guest");
  if(guest!=null)return guest.Id;
  guest=new AppUser{Role="Guest",Active=false,FullName="Khách vãng lai",UserName="guest-"+Guid.NewGuid().ToString("N")};
  s.Add(guest);
  await s.SaveAsync();
  return guest.Id;
 }
 // Xử lý đơn hàng / Giao hàng và ghi nhận COD — kiểm quyền tại nghiệp vụ.
 public Task<object> ActionAsync(Actor a,int id,OrderActionRequest r)=>s.WriteAsync<object>(async()=>{
  Rules.Role(a,"Customer","Employee","Delivery");var o=await Full().SingleOrDefaultAsync(x=>x.Id==id)??throw new BusinessException("NOT_FOUND","Không tìm thấy đơn.",404);
  if(a.Role=="Customer")Rules.Check(o.CustomerId==a.Id,"NOT_FOUND","Không tìm thấy đơn.",404);
  Rules.Version(o.Version,r.Version);var next=Rules.Next(o.Status,r.Action,a.Role);
  var current=o.Deliveries.OrderByDescending(x=>x.Sequence).FirstOrDefault();
  if(a.Role=="Delivery")Rules.Check(current!=null&&current.DriverId==a.Id&&current.Result=="Assigned"&&current.Id==r.DeliveryAttemptId,"FORBIDDEN","Bạn không được phân công lần giao này.",403);
  if(r.Action is "reject" or "fail" or "close-failed")Rules.Check(!string.IsNullOrWhiteSpace(r.Reason)&&r.Reason.Length<=500,"REASON_REQUIRED","Vui lòng nhập lý do (tối đa 500 ký tự).",400);
  if(r.Action=="assign") {
   Rules.Check(await s.Query<AppUser>().AnyAsync(x=>x.Id==r.DriverId&&x.Role=="Delivery"&&x.Active),"DRIVER_INVALID","Người giao hàng không hợp lệ.",400);
   o.Deliveries.Add(new DeliveryAttempt{DriverId=r.DriverId!.Value,AssignedById=a.Id,Sequence=(current?.Sequence??0)+1});
  }
  // Giao thành công — thu đủ COD, một phiếu thu, trừ tồn đúng một lần.
  if(r.Action=="complete") {
   Rules.Check(r.CollectedAmount==o.Total,"COD_MISMATCH","Tiền thực thu phải bằng tổng COD.");current!.Result="Completed";current.FinishedAt=DateTime.UtcNow;
   s.Add(new CodReceipt{OrderId=o.Id,DeliveryAttemptId=current.Id,DriverId=a.Id,Amount=o.Total});
  }
  if(r.Action=="fail"){current!.Result="Failed";current.Reason=r.Reason!;current.FinishedAt=DateTime.UtcNow;}
  // Kiểm bánh hoàn — số hỏng phải thuộc đơn, không âm và không vượt số đã giao.
  var damaged=r.DamagedLines??[];
  if(r.Action=="close-failed")Rules.Check(damaged.Select(x=>x.CakeId).Distinct().Count()==damaged.Count&&damaged.All(x=>x.Quantity>=0&&o.Lines.Any(l=>l.CakeId==x.CakeId&&x.Quantity<=l.Quantity)),"DAMAGED_INVALID","Số bánh hỏng không hợp lệ.",400);
  if(r.Action is "cancel" or "reject" or "complete" or "close-failed") {
   foreach(var l in o.Lines.OrderBy(x=>x.CakeId)) {
    var x=await s.Query<Inventory>().SingleAsync(x=>x.CakeId==l.CakeId);
    var loss=r.Action=="complete"?l.Quantity:r.Action=="close-failed"?(damaged.SingleOrDefault(y=>y.CakeId==l.CakeId)?.Quantity??0):0;
    Rules.Stock(x.OnHand-loss,x.Reserved-l.Quantity);x.OnHand-=loss;x.Reserved-=l.Quantity;x.Version++;
    s.Add(new StockMovement{CakeId=l.CakeId,ActorId=a.Id,OnHandDelta=-loss,ReservedDelta=-l.Quantity,Reason=r.Action+": "+o.Code});
   }
  }
  o.History.Add(new OrderHistory{ActorId=a.Id,From=o.Status,To=next,Reason=r.Reason??r.Action});o.Status=next;o.Version++;await s.SaveAsync();return View(o);
 });
}
