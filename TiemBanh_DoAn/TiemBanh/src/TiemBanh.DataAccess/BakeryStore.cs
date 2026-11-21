using System.Data;
using Microsoft.EntityFrameworkCore;
namespace TiemBanh.DataAccess;
// ==================== DAL — REPOSITORY VÀ RANH GIỚI GIAO DỊCH ====================
public class BakeryStore(BakeryDbContext db) {
 public IQueryable<T> Query<T>() where T:class => db.Set<T>();
 public void Add<T>(T entity) where T:class => db.Add(entity);
 public Task<int> SaveAsync() => db.SaveChangesAsync();
 // Kiểm tra sẵn sàng — DB kết nối được và migration không còn chờ áp dụng.
 public async Task<bool> ReadyAsync() {try{return await db.Database.CanConnectAsync()&&! (await db.Database.GetPendingMigrationsAsync()).Any();}catch{return false;}}
 public void Clear() => db.ChangeTracker.Clear();
 // Đặt hàng / Giao hàng / Đối soát COD — tuần tự hóa ghi để bảo vệ bất biến.
 public async Task<T> WriteAsync<T>(Func<Task<T>> action) {
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  try { var result=await action();await db.SaveChangesAsync();await tx.CommitAsync();return result; }
  catch { await tx.RollbackAsync();db.ChangeTracker.Clear();throw; }
 }
}
