using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TiemBanh.Business;
namespace TiemBanh.Api;
// ==================== UC11–UC13 / UC15 — QUẢN TRỊ VÀ ĐỐI SOÁT ====================
[ApiController,Route("api/management"),Authorize] public class ManagementController(AccountService accounts,CodService cod):BakeryController {
 [HttpGet("drivers")] public Task<List<UserView>> Drivers()=>accounts.DriversAsync(Actor);
 [HttpGet("users/{kind}")] public Task<List<UserView>> Users(string kind,string? search=null)=>accounts.UsersAsync(Actor,kind,search);
 [HttpPost("staff")] public Task<UserView> Staff(StaffRequest r)=>accounts.StaffAsync(Actor,r);
 [HttpPut("staff/{id:int}")] public Task<UserView> UpdateStaff(int id,StaffUpdateRequest r)=>accounts.UpdateStaffAsync(Actor,id,r);
 [HttpPut("users/{id:int}/active")] public Task<UserView> Active(int id,ActiveRequest r)=>accounts.ActiveAsync(Actor,id,r);
 [HttpGet("receipts")] public Task<object> Receipts()=>cod.ReceiptsAsync(Actor);
 [HttpGet("periods")] public Task<object> Periods()=>cod.PeriodsAsync(Actor);
 [HttpPost("periods")] public Task<object> Period(PeriodRequest r)=>cod.SavePeriodAsync(Actor,null,r);
 [HttpPut("periods/{id:int}")] public Task<object> UpdatePeriod(int id,PeriodRequest r)=>cod.SavePeriodAsync(Actor,id,r);
 [HttpPost("periods/{id:int}/{operation}")] public Task<object> PeriodAction(int id,string operation,VersionRequest r)=>cod.ActionAsync(Actor,id,operation,r.Version);
 [HttpGet("report")] public Task<object> Report()=>cod.ReportAsync(Actor);
}
