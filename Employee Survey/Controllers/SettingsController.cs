using Employee_Survey.Application;
using Employee_Survey.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_Survey.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _svc;
        private readonly IPermissionService _perms;
        private readonly IWebHostEnvironment _env;

        public SettingsController(ISettingsService svc, IPermissionService perms, IWebHostEnvironment env)
        {
            _svc = svc; _perms = perms; _env = env;
        }

        [HttpGet("/settings")]
        public async Task<IActionResult> Index()
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Settings_Edit))
                return Redirect("/auth/denied");

            var s = await _svc.GetAsync();
            return View("Index", s);
        }

        [HttpPost("/settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SystemSettings model, IFormFile? logo)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Settings_Edit))
                return Redirect("/auth/denied");

            var s = await _svc.GetAsync();
            s.SystemName = model.SystemName?.Trim() ?? s.SystemName;
            s.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (logo != null && logo.Length > 0)
            {
                Directory.CreateDirectory(Path.Combine(_env.WebRootPath!, "uploads", "logo"));
                var fileName = "logo" + Path.GetExtension(logo.FileName);
                var path = Path.Combine(_env.WebRootPath!, "uploads", "logo", fileName);
                using (var fs = System.IO.File.Create(path))
                    await logo.CopyToAsync(fs);
                s.LogoUrl = "/uploads/logo/" + fileName;
            }

            await _svc.SaveAsync(s);
            TempData["Msg"] = "Đã lưu cấu hình hệ thống.";
            return RedirectToAction(nameof(Index));
        }
    }
}
