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
        private readonly IEntityStore<User> _userRepo;

        public RolesController(IPermissionService perms, IEntityStore<User> userRepo)
        { _perms = perms; _userRepo = userRepo; }

        [HttpGet("/roles")]
        public async Task<IActionResult> Index()
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Roles_Assign))
                return Redirect("/auth/denied");

            await _perms.EnsureDefaultAsync();
            var maps = await _perms.GetAllAsync();
            ViewBag.AllCodes = PermissionCodes.All;
            var users = await _userRepo.GetAllAsync();
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

            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) { TempData["Err"] = "User không tồn tại."; return RedirectToAction(nameof(Index)); }
            u.Role = role;
            await _userRepo.UpsertAsync(x => x.Id == u.Id, u);
            TempData["Msg"] = $"Đã gán role {role} cho {u.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }
}

