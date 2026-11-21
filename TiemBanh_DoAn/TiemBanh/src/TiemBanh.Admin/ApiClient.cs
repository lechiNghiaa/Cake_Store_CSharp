using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace TiemBanh.Admin;
// Giao tiếp API — Admin không tham chiếu DAL/BLL hoặc truy cập SQL trực tiếp.
public sealed class ApiClient:IDisposable {
 readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(30)};
 public JsonElement User{get;private set;}
 public string BaseUrl{get;set;}=Environment.GetEnvironmentVariable("TIEMBANH_API_URL")??"http://localhost:8081/";
 public async Task<JsonElement> SendAsync(string path,HttpMethod? method=null,object? body=null){
  using var req=new HttpRequestMessage(method??HttpMethod.Get,new Uri(new Uri(BaseUrl.TrimEnd('/')+"/"),"api/"+path.TrimStart('/')));
  if(body!=null)req.Content=new StringContent(JsonSerializer.Serialize(body,new JsonSerializerOptions(JsonSerializerDefaults.Web)),Encoding.UTF8,"application/json");
  using var res=await http.SendAsync(req);var raw=await res.Content.ReadAsStringAsync();using var doc=JsonDocument.Parse(string.IsNullOrEmpty(raw)?"{}":raw);var data=doc.RootElement.Clone();
  if(!res.IsSuccessStatusCode)throw new InvalidOperationException(data.TryGetProperty("title",out var title)?title.GetString():"Không thể xử lý yêu cầu.");return data;
 }
 // Đăng nhập — token chỉ lưu trong bộ nhớ tiến trình.
 public async Task LoginAsync(string userName,string password){var result=await SendAsync("auth/login",HttpMethod.Post,new{userName,password});var user=result.GetProperty("user");if(user.GetProperty("role").GetString()!="Admin")throw new InvalidOperationException("Ứng dụng này chỉ dành cho Admin.");http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",result.GetProperty("token").GetString());User=user.Clone();}
 public async Task<JsonElement> UploadAsync(string file){using var form=new MultipartFormDataContent();await using var stream=File.OpenRead(file);form.Add(new StreamContent(stream),"file",Path.GetFileName(file));using var res=await http.PostAsync(new Uri(new Uri(BaseUrl),"api/cakes/images"),form);using var doc=JsonDocument.Parse(await res.Content.ReadAsStringAsync());if(!res.IsSuccessStatusCode)throw new InvalidOperationException(doc.RootElement.TryGetProperty("title",out var t)?t.GetString():"Tải ảnh thất bại.");return doc.RootElement.Clone();}
 public void Dispose()=>http.Dispose();
}
