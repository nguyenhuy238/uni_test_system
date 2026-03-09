using UniTestSystem.Application.Interfaces;
using System.Globalization;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Reports_View)]
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

            var facultyVm = await _svc.GetFacultyReportAsync(fromUtc, toUtc);
            var yearVm = await _svc.GetAcademicYearReportAsync(fromUtc, toUtc);

            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.CanExport = await _perms.HasAsync(User, PermissionCodes.Reports_Export);

            return View("Index", (facultyVm, yearVm));
        }

        [HttpGet("/reports/student-subject")]
        public async Task<IActionResult> StudentSubject(string userId, string? from = null, string? to = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            // [FIXED] IDOR Protection: Students can ONLY view their own report
            if (userId != currentUserId && !isAdmin)
            {
                return Redirect("/auth/denied");
            }

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-90) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetStudentSubjectReportAsync(userId, fromUtc, toUtc);
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            ViewBag.UserId = userId;
            ViewBag.UserName = u?.Name ?? userId;
            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.CanExport = await _perms.HasAsync(User, PermissionCodes.Reports_Export);

            return View("StudentSubject", vm);
        }

        // ============ EXPORT ============
        [HttpGet("/reports/export/xlsx")]
        public async Task<IActionResult> ExportXlsx(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var facultyVm = await _svc.GetFacultyReportAsync(fromUtc, toUtc);
            var yearVm = await _svc.GetAcademicYearReportAsync(fromUtc, toUtc);
            var bytes = _export.ExportFacultyYearExcel(facultyVm, yearVm, fromUtc, toUtc);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"academic-reports-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
        }

        [HttpGet("/reports/export/pdf")]
        public async Task<IActionResult> ExportPdf(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var facultyVm = await _svc.GetFacultyReportAsync(fromUtc, toUtc);
            var yearVm = await _svc.GetAcademicYearReportAsync(fromUtc, toUtc);
            var settings = await _settings.GetAsync();

            var bytes = _export.ExportFacultyYearPdf(facultyVm, yearVm, settings, fromUtc, toUtc);
            return File(bytes, "application/pdf", $"academic-reports-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf");
        }

        [HttpGet("/reports/student-subject/export/xlsx")]
        public async Task<IActionResult> ExportStudentSubjectXlsx(string userId, string? from = null, string? to = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export) || (userId != currentUserId && !isAdmin))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-90) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetStudentSubjectReportAsync(userId, fromUtc, toUtc);
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            var bytes = _export.ExportStudentSubjectExcel(vm, u?.Name ?? userId, fromUtc, toUtc);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"student-subject-{userId}-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
        }

        [HttpGet("/reports/student-subject/export/pdf")]
        public async Task<IActionResult> ExportStudentSubjectPdf(string userId, string? from = null, string? to = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export) || (userId != currentUserId && !isAdmin))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-90) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetStudentSubjectReportAsync(userId, fromUtc, toUtc);
            var u = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
            var settings = await _settings.GetAsync();

            var bytes = _export.ExportStudentSubjectPdf(vm, u?.Name ?? userId, settings, fromUtc, toUtc);
            return File(bytes, "application/pdf", $"student-subject-{userId}-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf");
        }
    }
}
