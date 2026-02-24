using System.Globalization;
using Employee_Survey.Application;
using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Employee_Survey.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Survey.Controllers
{
    [Authorize] // sẽ kiểm tra quyền chi tiết trong action
    public class ReportsController : Controller
    {
        private readonly ReportService _svc;
        private readonly IExportService _export;
        private readonly ISettingsService _settings;
        private readonly IRepository<User> _userRepo;
        private readonly IPermissionService _perms;

        public ReportsController(ReportService svc, IExportService export, ISettingsService settings, IRepository<User> userRepo, IPermissionService perms)
        {
            _svc = svc; _export = export; _settings = settings; _userRepo = userRepo; _perms = perms;
        }

        [HttpGet("/reports")]
        public async Task<IActionResult> Index(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_View))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var roleVm = await _svc.GetRoleReportAsync(fromUtc, toUtc);
            var levelVm = await _svc.GetLevelReportAsync(fromUtc, toUtc);

            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.CanExport = await _perms.HasAsync(User, PermissionCodes.Reports_Export);

            return View("Index", (roleVm, levelVm));
        }

        [HttpGet("/reports/user-skill")]
        public async Task<IActionResult> UserSkill(string userId, string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_View))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-90) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetUserSkillReportAsync(userId, fromUtc, toUtc);
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            ViewBag.UserId = userId;
            ViewBag.UserName = u?.Name ?? userId;
            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.CanExport = await _perms.HasAsync(User, PermissionCodes.Reports_Export);

            return View("UserSkill", vm);
        }

        // ============ EXPORT ============
        [HttpGet("/reports/export/xlsx")]
        public async Task<IActionResult> ExportXlsx(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var roleVm = await _svc.GetRoleReportAsync(fromUtc, toUtc);
            var levelVm = await _svc.GetLevelReportAsync(fromUtc, toUtc);
            var bytes = _export.ExportRoleLevelExcel(roleVm, levelVm, fromUtc, toUtc);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"reports-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
        }

        [HttpGet("/reports/export/pdf")]
        public async Task<IActionResult> ExportPdf(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var roleVm = await _svc.GetRoleReportAsync(fromUtc, toUtc);
            var levelVm = await _svc.GetLevelReportAsync(fromUtc, toUtc);
            var settings = await _settings.GetAsync();

            var bytes = _export.ExportRoleLevelPdf(roleVm, levelVm, settings, fromUtc, toUtc);
            return File(bytes, "application/pdf", $"reports-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf");
        }

        [HttpGet("/reports/user-skill/export/xlsx")]
        public async Task<IActionResult> ExportUserSkillXlsx(string userId, string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-90) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetUserSkillReportAsync(userId, fromUtc, toUtc);
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            var bytes = _export.ExportUserSkillExcel(vm, u?.Name ?? userId, fromUtc, toUtc);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"user-skill-{userId}-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
        }

        [HttpGet("/reports/user-skill/export/pdf")]
        public async Task<IActionResult> ExportUserSkillPdf(string userId, string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-90) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetUserSkillReportAsync(userId, fromUtc, toUtc);
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            var settings = await _settings.GetAsync();

            var bytes = _export.ExportUserSkillPdf(vm, u?.Name ?? userId, settings, fromUtc, toUtc);
            return File(bytes, "application/pdf", $"user-skill-{userId}-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf");
        }
    }
}
