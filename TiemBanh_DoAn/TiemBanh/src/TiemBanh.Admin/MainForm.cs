namespace TiemBanh.Admin;
public sealed class MainForm : Form {
  public MainForm() {
    Text = "Tiệm Bánh – Admin (khung)";
    Width = 720;
    Height = 420;
    Controls.Add(new Label { AutoSize = true, Text = "Giao diện Admin sẽ được phát triển theo UC10–UC13, UC15.", Left = 30, Top = 40 });
  }
}
