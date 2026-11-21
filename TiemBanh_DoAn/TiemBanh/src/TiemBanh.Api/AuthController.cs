using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TiemBanh.Business;
namespace TiemBanh.Api;
public abstract class BakeryController:ControllerBase {
 protected Actor Actor=>new(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),User.FindFirstValue(ClaimTypes.Role)!);
}
// ==================== UC01–UC03 — ĐĂNG NHẬP, ĐĂNG KÝ, HỒ SƠ ====================
[ApiController,Route("api/auth")] public class AuthController(AccountService accounts,IConfiguration cfg):BakeryController {
 [HttpPost("login"),EnableRateLimiting("auth")] public async Task<object> Login(LoginRequest r){
  var u=await accounts.LoginAsync(r);var token=new JwtSecurityToken("TiemBanh","TiemBanhClients",[new Claim(ClaimTypes.NameIdentifier,u.Id.ToString()),new Claim(ClaimTypes.Role,u.Role),new Claim("stamp",u.SecurityStamp)],expires:DateTime.UtcNow.AddHours(2),signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:SigningKey"]!)),SecurityAlgorithms.HmacSha256));
  return new{token=new JwtSecurityTokenHandler().WriteToken(token),user=AccountService.View(u)};
 }
 [HttpPost("register"),EnableRateLimiting("auth")] public Task<UserView> Register(RegisterRequest r)=>accounts.RegisterAsync(r);
 [HttpGet("me"),Authorize] public Task<UserView> Me()=>accounts.MeAsync(Actor);
 [HttpPut("me"),Authorize] public Task<UserView> Profile(ProfileRequest r)=>accounts.ProfileAsync(Actor,r);
}
