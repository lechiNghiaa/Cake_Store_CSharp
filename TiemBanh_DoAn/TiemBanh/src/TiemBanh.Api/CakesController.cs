using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TiemBanh.Business;
namespace TiemBanh.Api;
// ==================== UC04–UC05 / UC10 — DANH SÁCH, TÌM KIẾM, QUẢN LÝ BÁNH ====================
[ApiController,Route("api/cakes")] public class CakesController(CatalogService catalog,IConfiguration cfg,IWebHostEnvironment env):BakeryController {
 [HttpGet] public Task<object> List(string? search=null,int? categoryId=null,int page=1,int pageSize=12)=>catalog.ListAsync(search,categoryId,page,pageSize);
 [HttpGet("categories")] public Task<object> Categories()=>catalog.CategoriesAsync();
 [HttpGet("manage"),Authorize(Roles="Admin,Employee")] public Task<object> Manage(string? search=null)=>catalog.ListAsync(search,null,1,100,true);
 [HttpPost,Authorize(Roles="Admin")] public Task<object> Create(CakeRequest r)=>catalog.SaveAsync(Actor,null,r);
 [HttpPut("{id:int}"),Authorize(Roles="Admin")] public Task<object> Update(int id,CakeRequest r)=>catalog.SaveAsync(Actor,id,r);
 [HttpPut("{id:int}/stock"),Authorize(Roles="Admin")] public Task<object> Stock(int id,StockRequest r)=>catalog.StockAsync(Actor,id,r);
 // Quản lý ảnh bánh — xác thực signature file, giới hạn 5 MB và tên ngẫu nhiên.
 [HttpPost("images"),Authorize(Roles="Admin"),RequestSizeLimit(6*1024*1024)] public async Task<object> Image(IFormFile file){
  Rules.Check(file.Length>0&&file.Length<=5*1024*1024,"IMAGE_SIZE","Ảnh tối đa 5 MB.",400);
  using var ms=new MemoryStream();await file.CopyToAsync(ms);var data=ms.ToArray();
  var ext=data.Length>=3&&data[0]==255&&data[1]==216&&data[2]==255?"jpg":data.Length>=8&&data.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})?"png":data.Length>=12&&Encoding.ASCII.GetString(data,0,4)=="RIFF"&&Encoding.ASCII.GetString(data,8,4)=="WEBP"?"webp":null;
  Rules.Check(ext!=null,"IMAGE_FORMAT","Chỉ chấp nhận JPG, PNG hoặc WebP.",400);var name=Guid.NewGuid().ToString("N")+"."+ext;
  var root=cfg["Uploads:Root"]??Path.Combine(env.ContentRootPath,"uploads");await System.IO.File.WriteAllBytesAsync(Path.Combine(root,name),data);return new{url="/uploads/"+name};
 }
}
