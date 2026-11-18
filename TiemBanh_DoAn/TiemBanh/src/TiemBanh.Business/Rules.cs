namespace TiemBanh.Business;
// ==================== QUY TẮC NGHIỆP VỤ — CÁC PHÉP TÍNH NHỎ ====================
public static class Rules {
 public const decimal Shipping=20000m;
 // UC06–UC07 — Tính tiền bánh / Tính tổng COD (tên giống phần Giỏ hàng và đặt COD).
 public static decimal Total(IEnumerable<(decimal Price,int Quantity)> lines,decimal shipping) {
  var items=lines.ToList(); Check(items.Count>0&&shipping>=0&&items.All(x=>x.Price>=0&&x.Quantity>0),"INVALID_TOTAL","Dữ liệu tính tổng không hợp lệ.",400);
  return checked(items.Sum(x=>x.Price*x.Quantity)+shipping);
 }
 // UC07, UC10 — Kiểm tra tồn khả dụng / Giữ tồn / Điều chỉnh tồn.
 public static void Stock(int onHand,int reserved) => Check(onHand>=0&&reserved>=0&&reserved<=onHand,"STOCK_INSUFFICIENT","Không đủ tồn khả dụng.");
 public static void Version(long actual,long expected) => Check(actual==expected,"VERSION_CONFLICT","Dữ liệu đã thay đổi. Vui lòng tải lại.");
 // UC14, UC09 — Kiểm tra chuyển trạng thái đơn hàng.
 public static string Next(string current,string action,string role) => (current,action,role) switch {
  ("Pending","cancel","Customer")=>"Cancelled",("Pending","reject","Employee")=>"Cancelled",
  ("Pending","confirm","Employee")=>"Confirmed",("Confirmed","prepare","Employee")=>"Preparing",
  ("Preparing","assign","Employee")=>"Delivering",("DeliveryFailed","assign","Employee")=>"Delivering",
  ("Delivering","complete","Delivery")=>"Completed",("Delivering","fail","Delivery")=>"DeliveryFailed",
  ("DeliveryFailed","close-failed","Employee")=>"Cancelled",_=>throw new BusinessException("INVALID_ORDER_STATE","Vai trò hoặc trạng thái không cho phép thao tác.") };
 public static void Check(bool ok,string code,string message,int status=409) {if(!ok)throw new BusinessException(code,message,status);}
 public static void Role(Actor a,params string[] roles)=>Check(roles.Contains(a.Role),"FORBIDDEN","Bạn không có quyền thực hiện chức năng này.",403);
}
