namespace TiemBanh.Admin;
// ==================== ĐĂNG NHẬP / BIỂU MẪU NHẬP LIỆU ====================
public sealed class LoginForm:Form {
 public LoginForm(ApiClient client){
  Text="Tiệm Bánh — Đăng nhập Admin";Size=new Size(460,315);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
  var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=6};Controls.Add(panel);
  panel.Controls.Add(new Label{Text="Đăng nhập Admin",Font=new Font("Segoe UI",19,FontStyle.Bold),AutoSize=true});
  var name=new TextBox{Text="admin",Dock=DockStyle.Top};var password=new TextBox{UseSystemPasswordChar=true,Dock=DockStyle.Top};
  panel.Controls.Add(new Label{Text="Tên đăng nhập",AutoSize=true});panel.Controls.Add(name);panel.Controls.Add(new Label{Text="Mật khẩu",AutoSize=true});panel.Controls.Add(password);
  var button=new Button{Text="Đăng nhập",Dock=DockStyle.Top,Height=38};panel.Controls.Add(button);AcceptButton=button;
  button.Click+=async(_,_)=>{button.Enabled=false;try{await client.LoginAsync(name.Text.Trim(),password.Text);DialogResult=DialogResult.OK;Close();}catch(Exception ex){MessageBox.Show(ex.Message,"Đăng nhập",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{button.Enabled=true;}};
 }
}
public sealed class InputDialog:Form {
 readonly Dictionary<string,TextBox> fields=[];
 public InputDialog(string title,IEnumerable<(string Key,string Label,string Value)> values){
  Text=title;Width=540;Height=630;StartPosition=FormStartPosition.CenterParent;MinimumSize=new Size(420,350);
  var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};var list=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Padding=new Padding(20)};scroll.Controls.Add(list);Controls.Add(scroll);
  foreach(var(key,label,value)in values){list.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,8,0,3)});var tb=new TextBox{Text=value,Width=460,UseSystemPasswordChar=key=="password"};fields[key]=tb;list.Controls.Add(tb);}
  var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=60,Padding=new Padding(14),FlowDirection=FlowDirection.RightToLeft};var ok=new Button{Text="Lưu",DialogResult=DialogResult.OK};var cancel=new Button{Text="Hủy",DialogResult=DialogResult.Cancel};actions.Controls.Add(ok);actions.Controls.Add(cancel);Controls.Add(actions);AcceptButton=ok;CancelButton=cancel;
 }
 public string Get(string key)=>fields[key].Text.Trim();
 public static InputDialog? Open(IWin32Window owner,string title,params(string Key,string Label,string Value)[] values){var d=new InputDialog(title,values);if(d.ShowDialog(owner)==DialogResult.OK)return d;d.Dispose();return null;}
}
