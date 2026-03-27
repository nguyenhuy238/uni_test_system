using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.ViewModels.Feedback;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Audit_View)]
    public class AdminController : Controller
    {
        private readonly ISystemMaintenanceService _systemMaintenanceService;
        private readonly IResultsService _resultsService;

        public AdminController(
            ISystemMaintenanceService systemMaintenanceService,
            IResultsService resultsService)
        {
            _systemMaintenanceService = systemMaintenanceService;
            _resultsService = resultsService;
        }

        [HttpGet]
        public IActionResult Dashboard() => View();

        [HttpGet]
        public async Task<IActionResult> Feedbacks(string? testId = null)
        {
            var data = await _resultsService.GetAdminFeedbacksAsync(testId);
            var items = data.Items.Select(x => new AdminFeedbackItemVm
            {
                FeedbackId = x.FeedbackId,
                SessionId = x.SessionId,
                UserName = x.UserName,
                UserEmail = x.UserEmail,
                TestTitle = x.TestTitle,
                CreatedAt = x.CreatedAt,
                Rating = x.Rating,
                Content = x.Content
            }).ToList();

            ViewBag.Tests = data.Tests.Select(t => new { t.Id, t.Title }).ToList();
            ViewBag.SelectedTestId = testId;

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> ExportFeedbacksCsv(string? testId = null)
        {
            var data = await _resultsService.GetAdminFeedbacksAsync(testId);

            var sb = new StringBuilder();
            sb.AppendLine("CreatedAtUtc,TestTitle,StudentName,StudentEmail,Rating,Content,SessionId,FeedbackId");

            foreach (var x in data.Items.OrderByDescending(i => i.CreatedAt))
            {
                sb.AppendLine(
                    $"{x.CreatedAt:O}," +
                    $"{EscapeCsv(x.TestTitle)}," +
                    $"{EscapeCsv(x.UserName)}," +
                    $"{EscapeCsv(x.UserEmail)}," +
                    $"{x.Rating}," +
                    $"{EscapeCsv(x.Content)}," +
                    $"{EscapeCsv(x.SessionId)}," +
                    $"{EscapeCsv(x.FeedbackId)}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"feedbacks-admin-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetData()
        {
            await _systemMaintenanceService.ResetDatabaseAsync(reseed: true);
            TempData["Msg"] = "Đã reset toàn bộ cơ sở dữ liệu và seed lại dữ liệu mặc định.";
            return RedirectToAction(nameof(Dashboard));
        }

        private static string EscapeCsv(string? value)
        {
            var safe = (value ?? string.Empty).Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }
    }
}

