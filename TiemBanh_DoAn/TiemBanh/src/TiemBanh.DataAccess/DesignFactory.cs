using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace TiemBanh.DataAccess;
// Migration — chỉ dùng ở design time, không lưu thông tin đăng nhập thật.
public class DesignFactory:IDesignTimeDbContextFactory<BakeryDbContext> {
 public BakeryDbContext CreateDbContext(string[] args)=>new(new DbContextOptionsBuilder<BakeryDbContext>().UseSqlServer("Server=localhost;Database=TiemBanh;Integrated Security=true;TrustServerCertificate=true").Options);
}
