using UniTestSystem.Application.Interfaces;
using System.Globalization;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using UniTestSystem.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Reports_View)]
    public class ReportsController : Controller
    {
        private readonly ReportService _svc;
        private readonly IExportService _export;
        private readonly ISettingsService _settings;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Course> _courseRepo;
        private readonly IPermissionService _perms;

        public ReportsController(
            ReportService svc,
            IExportService export,
            ISettingsService settings,
            IRepository<User> userRepo,
            IRepository<Course> courseRepo,
            IPermissionService perms)
        {
            _svc = svc; _export = export; _settings = settings; _userRepo = userRepo; _courseRepo = courseRepo; _perms = perms;
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

            var roleValue = User.FindFirstValue(ClaimTypes.Role);
            var role = Enum.TryParse<Role>(roleValue, out var parsedRole) ? parsedRole : Role.Staff;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dashboardVm = await _svc.GetWidgetDashboardAsync(fromUtc, toUtc, role, currentUserId);

            var vm = new ReportsIndexVm
            {
                Faculty = facultyVm,
                AcademicYear = yearVm,
                Dashboard = dashboardVm
            };

            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.CanExport = await _perms.HasAsync(User, PermissionCodes.Reports_Export);

            return View("Index", vm);
        }

        [HttpGet("/reports/question-analytics")]
        public async Task<IActionResult> QuestionAnalytics(string? from = null, string? to = null, string? courseId = null, int minAttempts = 5)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_View))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var vm = await _svc.GetQuestionAnalyticsAsync(fromUtc, toUtc, courseId, minAttempts);
            var courses = await _courseRepo.GetAllAsync(c => !c.IsDeleted);

            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.MinAttempts = minAttempts < 1 ? 1 : minAttempts;
            ViewBag.CourseId = courseId;
            ViewBag.Courses = courses.OrderBy(c => c.Name).ToList();

            return View("QuestionAnalytics", vm);
        }

        [HttpGet("/reports/lecturer-performance")]
        public async Task<IActionResult> LecturerPerformance(string? from = null, string? to = null, string? lecturerId = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_View))
                return Redirect("/auth/denied");

            var toUtc = string.IsNullOrWhiteSpace(to) ? DateTime.UtcNow : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var fromUtc = string.IsNullOrWhiteSpace(from) ? toUtc.AddDays(-30) : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdminOrStaff = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            if (!isAdminOrStaff)
            {
                lecturerId = currentUserId;
            }
            else if (string.IsNullOrWhiteSpace(lecturerId))
            {
                lecturerId = null;
            }

            var vm = await _svc.GetLecturerPerformanceReportAsync(fromUtc, toUtc, lecturerId);

            var lecturers = await _userRepo.Query()
                .Where(x => x.Role == Role.Lecturer && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.LecturerId = lecturerId;
            ViewBag.Lecturers = lecturers;
            ViewBag.IsAdminOrStaff = isAdminOrStaff;

            return View("LecturerPerformance", vm);
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
