using TiemBanh.Business;
using Xunit;
namespace TiemBanh.Tests;
// Kiểm thử mức phần mềm — bất biến tiền, tồn và chuyển trạng thái nghiệp vụ.
public class RulesTests {
 [Fact] public void TinhTongCOD_DungGiaVaPhi(){Assert.Equal(120000m,Rules.Total(new[]{(50000m,2)},20000m));}
 [Fact] public void TinhTongCOD_ChinhXacSoThapPhan(){Assert.Equal(0.3m,Rules.Total(new[]{(0.1m,3)},0));}
 [Theory][InlineData(-1,0)][InlineData(1,2)][InlineData(0,-1)] public void TonKho_KhongChoViPham(int onHand,int reserved){Assert.Throws<BusinessException>(()=>Rules.Stock(onHand,reserved));}
 [Theory][InlineData(0,0)][InlineData(10,10)][InlineData(10,2)] public void TonKho_ChoPhepBienHopLe(int onHand,int reserved){Rules.Stock(onHand,reserved);}
 [Fact] public void TinhTongCOD_KhongNhanGioRong(){Assert.Throws<BusinessException>(()=>Rules.Total(Array.Empty<(decimal,int)>(),0));}
 [Fact] public void TinhTongCOD_KhongNhanSoLuongAm(){Assert.Throws<BusinessException>(()=>Rules.Total(new[]{(10m,-1)},0));}
 [Theory][InlineData("Pending","confirm","Employee","Confirmed")][InlineData("Pending","cancel","Customer","Cancelled")][InlineData("Pending","reject","Employee","Cancelled")][InlineData("Confirmed","prepare","Employee","Preparing")][InlineData("Preparing","assign","Employee","Delivering")][InlineData("Delivering","complete","Delivery","Completed")][InlineData("Delivering","fail","Delivery","DeliveryFailed")][InlineData("DeliveryFailed","assign","Employee","Delivering")][InlineData("DeliveryFailed","close-failed","Employee","Cancelled")]
 public void ChuyenTrangThai_DungQuyTrinh(string from,string action,string role,string to){Assert.Equal(to,Rules.Next(from,action,role));}
 [Theory][InlineData("Pending","confirm","Admin")][InlineData("Confirmed","cancel","Customer")][InlineData("Completed","complete","Delivery")][InlineData("Cancelled","cancel","Customer")]
 public void ChuyenTrangThai_ChanSaiQuyenVaKetThuc(string from,string action,string role){Assert.Throws<BusinessException>(()=>Rules.Next(from,action,role));}
 [Fact] public void PhienBan_ChanDuLieuCu(){Assert.Throws<BusinessException>(()=>Rules.Version(2,1));}
 [Fact] public void PhanQuyen_AdminKhongThayNhanVien(){Assert.Throws<BusinessException>(()=>Rules.Role(new Actor(1,"Admin"),"Employee"));}
}
