namespace TiemBanh.DataAccess;
// ==================== DỮ LIỆU DÙNG CHUNG — ánh xạ dữ liệu ba lớp ====================
public abstract class Entity { public int Id { get; set; } }
public abstract class Versioned : Entity { public long Version { get; set; } = 1; }
// UC01–UC03, UC12, UC15 — Đăng nhập, Hồ sơ cá nhân, Quản lý tài khoản.
public class AppUser : Versioned {
 public string UserName { get; set; } = ""; public string PasswordHash { get; set; } = "";
 public string FullName { get; set; } = ""; public string Phone { get; set; } = ""; public string Address { get; set; } = "";
 public string Role { get; set; } = "Customer"; public bool Active { get; set; } = true;
 public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
}
// UC04, UC05, UC10 — Danh sách bánh, Tìm kiếm bánh, Quản lý bánh và tồn kho.
public class Category : Entity { public string Name { get; set; } = ""; }
public class Cake : Versioned {
 public string Code { get; set; } = ""; public string Name { get; set; } = "";
 public int CategoryId { get; set; } public Category Category { get; set; } = null!;
 public decimal Price { get; set; } public string Description { get; set; } = "";
 public string Image { get; set; } = ""; public bool Active { get; set; } = true;
 public Inventory Inventory { get; set; } = null!;
}
public class Inventory { public int CakeId { get; set; } public Cake Cake { get; set; } = null!;
 public int OnHand { get; set; } public int Reserved { get; set; } public int Available { get; private set; } public long Version { get; set; } = 1; }
public class StockMovement : Entity { public int CakeId { get; set; } public Cake Cake { get; set; } = null!;
 public int ActorId { get; set; } public AppUser Actor { get; set; } = null!;
 public int OnHandDelta { get; set; } public int ReservedDelta { get; set; } public string Reason { get; set; } = ""; public DateTime At { get; set; } = DateTime.UtcNow; }
// UC07–UC09, UC14 — Đặt hàng COD, Theo dõi đơn hàng, Xử lý đơn hàng, Giao hàng.
public class Order : Versioned {
 public string Code { get; set; } = ""; public Guid RequestId { get; set; } public string RequestHash { get; set; } = "";
 public int CustomerId { get; set; } public AppUser Customer { get; set; } = null!;
 public string Recipient { get; set; } = ""; public string Phone { get; set; } = ""; public string Address { get; set; } = ""; public string Note { get; set; } = "";
 public decimal Shipping { get; set; } public decimal Total { get; set; } public string Status { get; set; } = "Pending";
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public List<OrderLine> Lines { get; set; } = []; public List<OrderHistory> History { get; set; } = []; public List<DeliveryAttempt> Deliveries { get; set; } = [];
}
public class OrderLine { public int OrderId { get; set; } public Order Order { get; set; } = null!;
 public int CakeId { get; set; } public Cake Cake { get; set; } = null!;
 public string CakeName { get; set; } = ""; public decimal UnitPrice { get; set; } public int Quantity { get; set; } }
public class OrderHistory : Entity { public int OrderId { get; set; } public Order Order { get; set; } = null!;
 public int ActorId { get; set; } public AppUser Actor { get; set; } = null!;
 public string From { get; set; } = ""; public string To { get; set; } = ""; public string Reason { get; set; } = ""; public DateTime At { get; set; } = DateTime.UtcNow; }
public class DeliveryAttempt : Entity { public int OrderId { get; set; } public Order Order { get; set; } = null!;
 public int DriverId { get; set; } public AppUser Driver { get; set; } = null!;
 public int AssignedById { get; set; } public AppUser AssignedBy { get; set; } = null!;
 public int Sequence { get; set; } public string Result { get; set; } = "Assigned"; public string Reason { get; set; } = "";
 public DateTime AssignedAt { get; set; } = DateTime.UtcNow; public DateTime? FinishedAt { get; set; } }
// UC11, UC13 — Đối soát COD, Báo cáo thống kê.
public class CodReceipt : Entity { public int OrderId { get; set; } public Order Order { get; set; } = null!;
 public int DeliveryAttemptId { get; set; } public DeliveryAttempt DeliveryAttempt { get; set; } = null!;
 public int DriverId { get; set; } public AppUser Driver { get; set; } = null!;
 public decimal Amount { get; set; } public DateTime CollectedAt { get; set; } = DateTime.UtcNow; }
public class CodPeriod : Versioned { public DateTime Date { get; set; } = DateTime.UtcNow.Date;
 public int CreatedById { get; set; } public AppUser CreatedBy { get; set; } = null!;
 public int? ApprovedById { get; set; } public AppUser? ApprovedBy { get; set; }
 public string Status { get; set; } = "Submitted"; public DateTime? ApprovedAt { get; set; } public DateTime? LockedAt { get; set; }
 public List<CodHandover> Items { get; set; } = []; }
public class CodHandover : Entity { public int PeriodId { get; set; } public CodPeriod Period { get; set; } = null!;
 public int ReceiptId { get; set; } public CodReceipt Receipt { get; set; } = null!;
 public decimal Expected { get; set; } public decimal Received { get; set; } public string Note { get; set; } = ""; }
