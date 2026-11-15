using Microsoft.EntityFrameworkCore;
namespace TiemBanh.DataAccess;
// ==================== DAL — LƯU TRỮ SQL SERVER VÀ RÀNG BUỘC ====================
public class BakeryDbContext(DbContextOptions<BakeryDbContext> options) : DbContext(options) {
 protected override void OnModelCreating(ModelBuilder m) {
  foreach (var t in new[]{typeof(AppUser),typeof(Category),typeof(Cake),typeof(StockMovement),typeof(Order),typeof(OrderHistory),typeof(DeliveryAttempt),typeof(CodReceipt),typeof(CodPeriod),typeof(CodHandover)}) m.Entity(t);
  m.Entity<Inventory>().HasKey(x=>x.CakeId); m.Entity<Inventory>().HasOne(x=>x.Cake).WithOne(x=>x.Inventory).HasForeignKey<Inventory>(x=>x.CakeId);
  m.Entity<OrderLine>().HasKey(x=>new{x.OrderId,x.CakeId});
  // UC07 — Chống gửi lặp đơn hàng và giữ giá tại thời điểm đặt.
  m.Entity<AppUser>().HasIndex(x=>x.UserName).IsUnique(); m.Entity<Category>().HasIndex(x=>x.Name).IsUnique(); m.Entity<Cake>().HasIndex(x=>x.Code).IsUnique();
  m.Entity<Order>().HasIndex(x=>new{x.CustomerId,x.RequestId}).IsUnique(); m.Entity<Order>().HasIndex(x=>x.Code).IsUnique();
  m.Entity<Order>().HasIndex(x=>new{x.Status,x.CreatedAt}); m.Entity<Order>().HasIndex(x=>new{x.CustomerId,x.CreatedAt});
  m.Entity<DeliveryAttempt>().HasIndex(x=>new{x.OrderId,x.Sequence}).IsUnique();
  m.Entity<DeliveryAttempt>().HasOne(x=>x.AssignedBy).WithMany().HasForeignKey(x=>x.AssignedById);
  m.Entity<CodPeriod>().HasOne(x=>x.CreatedBy).WithMany().HasForeignKey(x=>x.CreatedById);
  m.Entity<CodPeriod>().HasOne(x=>x.ApprovedBy).WithMany().HasForeignKey(x=>x.ApprovedById);
  // UC09, UC11 — Một phiếu thu mỗi đơn/lần giao; mỗi phiếu chỉ vào một kỳ.
  m.Entity<CodReceipt>().HasIndex(x=>x.OrderId).IsUnique(); m.Entity<CodReceipt>().HasIndex(x=>x.DeliveryAttemptId).IsUnique();
  m.Entity<CodHandover>().HasIndex(x=>x.ReceiptId).IsUnique();
  m.Entity<Inventory>().Property(x=>x.Available).HasComputedColumnSql("[OnHand]-[Reserved]");
  m.Entity<Inventory>().ToTable(t=>t.HasCheckConstraint("CK_Inventory_Quantity","[OnHand]>=0 AND [Reserved]>=0 AND [Reserved]<=[OnHand]"));
  m.Entity<OrderLine>().ToTable(t=>t.HasCheckConstraint("CK_OrderLine_Quantity","[Quantity]>0 AND [UnitPrice]>=0"));
  m.Entity<Cake>().ToTable(t=>t.HasCheckConstraint("CK_Cake_Price","[Price]>=0"));
  m.Entity<Order>().ToTable(t=>t.HasCheckConstraint("CK_Order_Money","[Shipping]>=0 AND [Total]>=0"));
  m.Entity<CodReceipt>().ToTable(t=>t.HasCheckConstraint("CK_Receipt_Amount","[Amount]>=0"));
  m.Entity<CodHandover>().ToTable(t=>t.HasCheckConstraint("CK_Handover_Amount","[Expected]>=0 AND [Received]>=0"));
  foreach(var entity in m.Model.GetEntityTypes()) {
   foreach(var prop in entity.GetProperties()) {
    if(prop.ClrType==typeof(string)) prop.SetMaxLength(prop.Name is "PasswordHash" or "Description" or "Address" or "Note" or "Reason" ? 1000:120);
    if(prop.ClrType==typeof(decimal)) { prop.SetPrecision(18); prop.SetScale(2); }
    if(prop.Name=="Version") prop.IsConcurrencyToken=true;
   }
   foreach(var fk in entity.GetForeignKeys()) fk.DeleteBehavior=DeleteBehavior.Restrict;
  }
 }
}
