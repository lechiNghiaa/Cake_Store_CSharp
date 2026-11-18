using System.ComponentModel.DataAnnotations;
namespace TiemBanh.Business;
// ==================== HỢP ĐỒNG CHỨC NĂNG — dùng chung tên với giao diện ====================
public record Actor(int Id,string Role);
public record UserView(int Id,string UserName,string FullName,string Phone,string Address,string Role,bool Active,long Version);
public record LoginRequest([Required,StringLength(60)] string UserName,[Required,StringLength(100)] string Password);
public record RegisterRequest([Required,StringLength(60,MinimumLength=3)] string UserName,[Required,StringLength(100,MinimumLength=8)] string Password,[Required,StringLength(100)] string FullName,[StringLength(30)] string Phone="",[StringLength(500)] string Address="");
public record ProfileRequest([Required,StringLength(100)] string FullName,[StringLength(30)] string Phone,[StringLength(500)] string Address);
public record StaffRequest([Required,StringLength(60)] string UserName,[Required,StringLength(100,MinimumLength=8)] string Password,[Required,StringLength(100)] string FullName,[Required] string Role,[StringLength(30)] string Phone="");
public record StaffUpdateRequest([Required,StringLength(100)] string FullName,[StringLength(30)] string Phone,[Required] string Role,long Version);
public record ActiveRequest(bool Active,long Version);
public record CakeRequest([Required,StringLength(20)] string Code,[Required,StringLength(100)] string Name,int CategoryId,[Range(0,100000000)] decimal Price,[StringLength(1000)] string Description="",[StringLength(120)] string Image="",bool Active=true,long Version=0);
public record StockRequest([Range(0,int.MaxValue)] int OnHand,long Version,[Required,StringLength(500)] string Reason);
public record LineRequest(int CakeId,[Range(1,10000)] int Quantity);
public record CreateOrderRequest(Guid RequestId,[Required,MinLength(1),MaxLength(100)] List<LineRequest> Lines,[Required,StringLength(100)] string Recipient,[Required,StringLength(30)] string Phone,[Required,StringLength(500)] string Address,[StringLength(500)] string Note="");
public record OrderActionRequest([Required] string Action,long Version,int? DriverId=null,int? DeliveryAttemptId=null,decimal? CollectedAmount=null,string? Reason=null,List<LineRequest>? DamagedLines=null);
public record HandoverRequest(int ReceiptId,[Range(0,100000000)] decimal Received,[StringLength(500)] string Note="");
public record PeriodRequest([Required,MinLength(1)] List<HandoverRequest> Items,long Version=0);
public record VersionRequest(long Version);
public class BusinessException(string code,string message,int status=409):Exception(message) { public string Code{get;}=code;public int Status{get;}=status; }
