namespace TiemBanh.Admin;
// ==================== PRESENTATION — WINFORMS ADMIN (.NET 9 WINDOWS) ====================
internal static class Program {
 [STAThread] static void Main(){ApplicationConfiguration.Initialize();using var client=new ApiClient();using var login=new LoginForm(client);if(login.ShowDialog()==DialogResult.OK)Application.Run(new MainForm(client));}
}
