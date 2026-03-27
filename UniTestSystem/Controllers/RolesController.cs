using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Roles_Assign)]
    public class RolesController : Controller
    {
        private readonly IPermissionService _perms;
        private readonly IUserAdministrationService _userAdministrationService;

        public RolesController(IPermissionService perms, IUserAdministrationService userAdministrationService)
        { _perms = perms; _userAdministrationService = userAdministrationService; }

        [HttpGet("/roles")]
        public async Task<IActionResult> Index()
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Roles_Assign))
                return Redirect("/auth/denied");

            await _perms.EnsureDefaultAsync();
            var maps = await _perms.GetAllAsync();
            ViewBag.AllCodes = PermissionCodes.All;
            var users = await _userAdministrationService.GetAllUsersAsync();
            ViewBag.Users = users.OrderBy(u => u.Name).ToList();
            return View("Index", maps);
        }

        [HttpPost("/roles/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Role role, string[] permissions)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Roles_Assign))
                return Redirect("/auth/denied");

            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            await _perms.UpsertAsync(role, permissions, actor);
            TempData["Msg"] = $"Đã cập nhật quyền cho role {role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/roles/assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, Role role)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Roles_Assign))
                return Redirect("/auth/denied");

            var u = await _userAdministrationService.GetUserByIdAsync(userId);
            if (u == null) { TempData["Err"] = "User không tồn tại."; return RedirectToAction(nameof(Index)); }
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "system";
            await _userAdministrationService.AssignRoleAsync(userId, role, ip);
            TempData["Msg"] = $"Đã gán role {role} cho {u.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }
}

