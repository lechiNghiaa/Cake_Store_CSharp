using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
namespace TiemBanh.Admin;
// ==================== ĐIỀU HƯỚNG ADMIN — UC03 / UC10–UC13 / UC15 ====================
public sealed class MainForm:Form {
 readonly ApiClient api;readonly DataGridView grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells};
 readonly FlowLayoutPanel actions=new(){Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(12)};
 readonly Label summary=new(){Dock=DockStyle.Top,Height=65,Padding=new Padding(20,12,10,5),Font=new Font("Segoe UI",13,FontStyle.Bold)};
 readonly ToolStripStatusLabel status=new("Sẵn sàng");readonly TextBox search=new(){Width=220,PlaceholderText="Tìm kiếm theo tên hoặc mã"};
 List<JsonElement> rows=[];string screen="report";
 public MainForm(ApiClient client){api=client;Text="Tiệm Bánh — Quản trị hệ thống";Size=new Size(1220,800);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);
  var sidebar=new FlowLayoutPanel{Dock=DockStyle.Left,Width=220,FlowDirection=FlowDirection.TopDown,Padding=new Padding(12),BackColor=Color.FromArgb(235,240,230)};
  foreach(var(key,title)in new[]{("report","Báo cáo thống kê"),("cakes","Quản lý bánh và tồn kho"),("customers","Quản lý khách hàng"),("staff","Quản lý nhân viên"),("cod","Đối soát COD"),("profile","Hồ sơ cá nhân")}){var b=new Button{Text=title,Width=190,Height=48,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,5,0,5)};b.Click+=async(_,_)=>await Safe(async()=>{screen=key;search.Text="";await LoadScreen();});sidebar.Controls.Add(b);}
  var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(grid);body.Controls.Add(actions);body.Controls.Add(summary);Controls.Add(body);Controls.Add(sidebar);var strip=new StatusStrip();strip.Items.Add(status);Controls.Add(strip);Shown+=async(_,_)=>await Safe(LoadScreen);
 }
 async Task Safe(Func<Task> action){Enabled=false;try{status.Text="Đang xử lý…";await action();status.Text="Sẵn sàng";}catch(Exception ex){status.Text="Thao tác chưa hoàn tất";MessageBox.Show(this,ex.Message,"Tiệm Bánh",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{Enabled=true;}}
 void Button(string text,Func<Task> action){var b=new Button{Text=text,AutoSize=true,Height=36,Margin=new Padding(4)};b.Click+=async(_,_)=>await Safe(action);actions.Controls.Add(b);}
 static string S(JsonElement e,string key)=>e.TryGetProperty(key,out var p)&&p.ValueKind!=JsonValueKind.Null?p.ToString():"";
 static int I(JsonElement e,string key)=>e.GetProperty(key).GetInt32();static long V(JsonElement e,string key="version")=>e.GetProperty(key).GetInt64();
 JsonElement Selected()=>grid.CurrentRow!=null&&grid.CurrentRow.Index<rows.Count?rows[grid.CurrentRow.Index]:throw new InvalidOperationException("Vui lòng chọn một dòng dữ liệu.");
 // Bảng dữ liệu — chỉ hiển thị trường scalar, giữ bản JSON để gửi đúng Version.
 void Bind(IEnumerable<JsonElement> data){rows=data.Select(x=>x.Clone()).ToList();var table=new DataTable();if(rows.Count>0){var props=rows[0].EnumerateObject().Where(p=>p.Value.ValueKind is not JsonValueKind.Array and not JsonValueKind.Object).ToList();foreach(var p in props)table.Columns.Add(p.Name);foreach(var row in rows)table.Rows.Add(props.Select(p=>(object)S(row,p.Name)).ToArray());}grid.DataSource=table;}
 async Task LoadScreen(){
  actions.Controls.Clear();summary.Text=screen switch{"cakes"=>"Quản lý bánh và tồn kho","customers"=>"Quản lý khách hàng","staff"=>"Quản lý nhân viên","cod"=>"Đối soát COD","profile"=>"Hồ sơ cá nhân",_=>"Báo cáo thống kê"};
  Button("Làm mới",LoadScreen);
  if(screen is "cakes" or "customers" or "staff"){actions.Controls.Add(search);Button(screen=="cakes"?"Tìm kiếm bánh":"Tìm kiếm tài khoản",LoadScreen);}
  switch(screen){
   case "report":
    var report=await api.SendAsync("management/report");summary.Text=$"Báo cáo thống kê  |  Doanh thu COD: {S(report,"revenue")} ₫  |  Bánh khả dụng: {S(report,"available")}";Bind(report.GetProperty("orderCounts").EnumerateArray());Button("Xuất báo cáo CSV",ExportReport);break;
   case "cakes":var cakes=await api.SendAsync("cakes/manage?search="+Uri.EscapeDataString(search.Text));Bind(cakes.GetProperty("items").EnumerateArray());Button("Thêm bánh",()=>EditCake(false));Button("Sửa bánh",()=>EditCake(true));Button("Điều chỉnh tồn",Stock);Button("Tải ảnh bánh",Image);break;
   case "customers":case "staff":var users=await api.SendAsync("management/users/"+screen+"?search="+Uri.EscapeDataString(search.Text));Bind(users.EnumerateArray());if(screen=="staff"){Button("Tạo nhân viên",CreateStaff);Button("Sửa nhân viên",EditStaff);}Button("Khóa / Mở tài khoản",ToggleActive);break;
   case "cod":var periods=await api.SendAsync("management/periods");Bind(periods.EnumerateArray());Button("Xem chi tiết kỳ",PeriodDetails);Button("Duyệt kỳ COD",()=>PeriodAction("approve"));Button("Khóa kỳ COD",()=>PeriodAction("lock"));break;
   case "profile":var me=await api.SendAsync("auth/me");Bind([me]);Button("Sửa hồ sơ cá nhân",Profile);break;
  }
 }
 // UC10 — Thêm bánh / Sửa bánh; danh mục lấy từ API để tránh tự đoán mã.
 async Task EditCake(bool edit){var c=edit?Selected():default;var cats=await api.SendAsync("cakes/categories");var hint=string.Join(", ",cats.EnumerateArray().Select(x=>S(x,"id")+"="+S(x,"name")));
  using var d=InputDialog.Open(this,edit?"Sửa bánh":"Thêm bánh",("code","Mã bánh",edit?S(c,"code"):""),("name","Tên bánh",edit?S(c,"name"):""),("categoryId","Mã danh mục ("+hint+")",edit?S(c,"categoryId"):"1"),("price","Giá bánh (số, dùng dấu chấm phần thập phân)",edit?S(c,"price"):"0"),("description","Mô tả",edit?S(c,"description"):""),("active","Đang bán: true / false",edit?S(c,"active"):"true"));if(d==null)return;
  await api.SendAsync(edit?"cakes/"+I(c,"id"):"cakes",edit?HttpMethod.Put:HttpMethod.Post,new{code=d.Get("code"),name=d.Get("name"),categoryId=int.Parse(d.Get("categoryId")),price=decimal.Parse(d.Get("price"),CultureInfo.InvariantCulture),description=d.Get("description"),active=bool.Parse(d.Get("active")),image=edit?S(c,"image"):"",version=edit?V(c):0});await LoadScreen();
 }
 // UC10 — Điều chỉnh tồn, bắt buộc lý do và dùng StockVersion.
 async Task Stock(){var c=Selected();using var d=InputDialog.Open(this,"Điều chỉnh tồn",("onHand","Tồn thực có",S(c,"onHand")),("reason","Lý do điều chỉnh",""));if(d==null)return;await api.SendAsync("cakes/"+I(c,"id")+"/stock",HttpMethod.Put,new{onHand=int.Parse(d.Get("onHand")),reason=d.Get("reason"),version=V(c,"stockVersion")});await LoadScreen();}
 // UC10 — Tải ảnh bánh, sau upload liên kết URL ảnh với bánh đã chọn.
 async Task Image(){var c=Selected();using var file=new OpenFileDialog{Filter="Ảnh bánh|*.jpg;*.jpeg;*.png;*.webp"};if(file.ShowDialog(this)!=DialogResult.OK)return;var image=await api.UploadAsync(file.FileName);await api.SendAsync("cakes/"+I(c,"id"),HttpMethod.Put,new{code=S(c,"code"),name=S(c,"name"),categoryId=I(c,"categoryId"),price=c.GetProperty("price").GetDecimal(),description=S(c,"description"),active=c.GetProperty("active").GetBoolean(),image=S(image,"url"),version=V(c)});await LoadScreen();}
 // UC12 — Tạo nhân viên / Sửa nhân viên; không tạo Admin công khai.
 async Task CreateStaff(){using var d=InputDialog.Open(this,"Tạo nhân viên",("userName","Tên đăng nhập",""),("password","Mật khẩu (ít nhất 8 ký tự)",""),("fullName","Họ tên",""),("phone","Số điện thoại",""),("role","Vai trò: Employee / Delivery","Employee"));if(d==null)return;await api.SendAsync("management/staff",HttpMethod.Post,new{userName=d.Get("userName"),password=d.Get("password"),fullName=d.Get("fullName"),phone=d.Get("phone"),role=d.Get("role")});await LoadScreen();}
 async Task EditStaff(){var u=Selected();using var d=InputDialog.Open(this,"Sửa nhân viên",("fullName","Họ tên",S(u,"fullName")),("phone","Số điện thoại",S(u,"phone")),("role","Vai trò: Employee / Delivery",S(u,"role")));if(d==null)return;await api.SendAsync("management/staff/"+I(u,"id"),HttpMethod.Put,new{fullName=d.Get("fullName"),phone=d.Get("phone"),role=d.Get("role"),version=V(u)});await LoadScreen();}
 // UC15 — Khóa / Mở tài khoản; API tăng SecurityStamp để chặn token cũ.
 async Task ToggleActive(){var u=Selected();if(MessageBox.Show(this,"Xác nhận đổi trạng thái tài khoản "+S(u,"userName")+"?","Quản lý tài khoản",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;await api.SendAsync("management/users/"+I(u,"id")+"/active",HttpMethod.Put,new{active=!u.GetProperty("active").GetBoolean(),version=V(u)});await LoadScreen();}
 // UC11 — Xem từng khoản / Duyệt kỳ COD / Khóa kỳ COD (Admin không lập kỳ).
 Task PeriodDetails(){var p=Selected();MessageBox.Show(this,string.Join(Environment.NewLine,p.GetProperty("items").EnumerateArray().Select(x=>$"Phiếu #{S(x,"receiptId")}: phải thu {S(x,"expected")}; thực nhận {S(x,"received")}; {S(x,"note")}")),"Chi tiết kỳ COD");return Task.CompletedTask;}
 async Task PeriodAction(string action){var p=Selected();if(MessageBox.Show(this,"Xác nhận "+(action=="approve"?"duyệt":"khóa")+" kỳ #"+I(p,"id")+"?","Đối soát COD",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;await api.SendAsync("management/periods/"+I(p,"id")+"/"+action,HttpMethod.Post,new{version=V(p)});await LoadScreen();}
 // UC03 — Hồ sơ cá nhân trên WinForms dùng cùng API với Web.
 async Task Profile(){var u=Selected();using var d=InputDialog.Open(this,"Hồ sơ cá nhân",("fullName","Họ tên",S(u,"fullName")),("phone","Số điện thoại",S(u,"phone")),("address","Địa chỉ",S(u,"address")));if(d==null)return;await api.SendAsync("auth/me",HttpMethod.Put,new{fullName=d.Get("fullName"),phone=d.Get("phone"),address=d.Get("address")});await LoadScreen();}
 // UC13 — Xuất báo cáo CSV; số doanh thu do API tính theo quy tắc COD.
 async Task ExportReport(){var r=await api.SendAsync("management/report");using var dialog=new SaveFileDialog{Filter="CSV|*.csv",FileName="BaoCao_TiemBanh.csv"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;var csv=new StringBuilder("Chỉ tiêu,Giá trị\r\n").AppendLine("Doanh thu COD,"+S(r,"revenue")).AppendLine("Bánh khả dụng,"+S(r,"available"));foreach(var c in r.GetProperty("orderCounts").EnumerateArray())csv.AppendLine(S(c,"status")+","+S(c,"count"));await File.WriteAllTextAsync(dialog.FileName,csv.ToString(),new UTF8Encoding(true));}
}
