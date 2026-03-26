using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetData()
        {
            await _systemMaintenanceService.ResetDatabaseAsync(reseed: true);
            TempData["Msg"] = "Đã reset toàn bộ cơ sở dữ liệu và seed lại dữ liệu mặc định.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}

