using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TiemBanh.Business;
namespace TiemBanh.Api;
// ==================== UC07–UC09 / UC14 — ĐẶT, THEO DÕI, XỬ LÝ VÀ GIAO ĐƠN ====================
[ApiController,Route("api/orders"),Authorize] public class OrdersController(OrderService orders):BakeryController {
 [HttpGet] public Task<object> List()=>orders.ListAsync(Actor);
 [HttpPost] public async Task<IActionResult> Create(CreateOrderRequest r)=>StatusCode(201,await orders.PlaceAsync(Actor,r));
 [HttpPost("guest"),AllowAnonymous,EnableRateLimiting("auth")]
 public async Task<IActionResult> Guest(CreateOrderRequest r){
  if(User.Identity?.IsAuthenticated==true)return Forbid();
  return StatusCode(201,await orders.PlaceAsync(null,r));
 }
 [HttpPost("{id:int}/actions")] public Task<object> Action(int id,OrderActionRequest r)=>orders.ActionAsync(Actor,id,r);
}
