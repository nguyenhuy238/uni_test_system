using UniTestSystem.Application.Interfaces;
using System.Globalization;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniTestSystem.Application;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Reports_View)]
    public class ReportsController : Controller
    {
        private readonly IReportsUseCaseService _reportsUseCaseService;
        private readonly IPermissionService _perms;

        public ReportsController(
            IReportsUseCaseService reportsUseCaseService,
            IPermissionService perms)
        {
            _reportsUseCaseService = reportsUseCaseService;
            _perms = perms;
        }

        [HttpGet("/reports")]
        public async Task<IActionResult> Index(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_View))
                return Redirect("/auth/denied");

            var (fromUtc, toUtc) = ResolveRange(from, to, 30);

            var roleValue = User.FindFirstValue(ClaimTypes.Role);
            var role = Enum.TryParse<Role>(roleValue, out var parsedRole) ? parsedRole : Role.Staff;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vm = await _reportsUseCaseService.GetIndexVmAsync(fromUtc, toUtc, role, currentUserId);

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

            var (fromUtc, toUtc) = ResolveRange(from, to, 30);
            var vm = await _reportsUseCaseService.GetQuestionAnalyticsVmAsync(fromUtc, toUtc, courseId, minAttempts);
            var courses = await _reportsUseCaseService.GetActiveCoursesAsync();

            ViewBag.From = fromUtc.ToString("yyyy-MM-dd");
            ViewBag.To = toUtc.ToString("yyyy-MM-dd");
            ViewBag.MinAttempts = minAttempts < 1 ? 1 : minAttempts;
            ViewBag.CourseId = courseId;
            ViewBag.Courses = courses;

            return View("QuestionAnalytics", vm);
        }

        [HttpGet("/reports/lecturer-performance")]
        public async Task<IActionResult> LecturerPerformance(string? from = null, string? to = null, string? lecturerId = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_View))
                return Redirect("/auth/denied");

            var (fromUtc, toUtc) = ResolveRange(from, to, 30);

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

            var vm = await _reportsUseCaseService.GetLecturerPerformanceVmAsync(fromUtc, toUtc, lecturerId);
            var lecturers = await _reportsUseCaseService.GetActiveLecturersAsync();

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
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            // [FIXED] IDOR Protection: Students can ONLY view their own report
            if (userId != currentUserId && !isAdmin)
            {
                return Redirect("/auth/denied");
            }

            var (fromUtc, toUtc) = ResolveRange(from, to, 90);

            var vm = await _reportsUseCaseService.GetStudentSubjectVmAsync(userId, fromUtc, toUtc);
            var user = await _reportsUseCaseService.GetUserByIdAsync(userId);
            ViewBag.UserId = userId;
            ViewBag.UserName = user?.Name ?? userId;
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

            var (fromUtc, toUtc) = ResolveRange(from, to, 30);
            var bytes = await _reportsUseCaseService.ExportFacultyYearExcelAsync(fromUtc, toUtc);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"academic-reports-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
        }

        [HttpGet("/reports/export/pdf")]
        public async Task<IActionResult> ExportPdf(string? from = null, string? to = null)
        {
            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export))
                return Redirect("/auth/denied");

            var (fromUtc, toUtc) = ResolveRange(from, to, 30);
            var bytes = await _reportsUseCaseService.ExportFacultyYearPdfAsync(fromUtc, toUtc);
            return File(bytes, "application/pdf", $"academic-reports-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf");
        }

        [HttpGet("/reports/student-subject/export/xlsx")]
        public async Task<IActionResult> ExportStudentSubjectXlsx(string userId, string? from = null, string? to = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export) || (userId != currentUserId && !isAdmin))
                return Redirect("/auth/denied");

            var (fromUtc, toUtc) = ResolveRange(from, to, 90);
            var bytes = await _reportsUseCaseService.ExportStudentSubjectExcelAsync(userId, fromUtc, toUtc);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"student-subject-{userId}-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
        }

        [HttpGet("/reports/student-subject/export/pdf")]
        public async Task<IActionResult> ExportStudentSubjectPdf(string userId, string? from = null, string? to = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(Role.Admin.ToString()) || User.IsInRole(Role.Staff.ToString());

            if (!await _perms.HasAsync(User, PermissionCodes.Reports_Export) || (userId != currentUserId && !isAdmin))
                return Redirect("/auth/denied");

            var (fromUtc, toUtc) = ResolveRange(from, to, 90);
            var bytes = await _reportsUseCaseService.ExportStudentSubjectPdfAsync(userId, fromUtc, toUtc);
            return File(bytes, "application/pdf", $"student-subject-{userId}-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf");
        }

        private static (DateTime FromUtc, DateTime ToUtc) ResolveRange(string? from, string? to, int defaultDays)
        {
            var toUtc = string.IsNullOrWhiteSpace(to)
                ? DateTime.UtcNow
                : DateTime.Parse(to, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var fromUtc = string.IsNullOrWhiteSpace(from)
                ? toUtc.AddDays(-Math.Abs(defaultDays))
                : DateTime.Parse(from, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            return (fromUtc, toUtc);
        }
    }
}
