using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Controllers
{
    [Authorize(Policy = PermissionCodes.Audit_View)]
    public class AdminController : Controller
    {
        private readonly ISystemMaintenanceService _systemMaintenanceService;
        private readonly IRepository<Feedback> _feedbackRepo;
        private readonly IRepository<Session> _sessionRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Test> _testRepo;

        public AdminController(
            ISystemMaintenanceService systemMaintenanceService,
            IRepository<Feedback> feedbackRepo,
            IRepository<Session> sessionRepo,
            IRepository<User> userRepo,
            IRepository<Test> testRepo)
        {
            _systemMaintenanceService = systemMaintenanceService;
            _feedbackRepo = feedbackRepo;
            _sessionRepo = sessionRepo;
            _userRepo = userRepo;
            _testRepo = testRepo;
        }

        [HttpGet]
        public IActionResult Dashboard() => View();

        [HttpGet]
        public async Task<IActionResult> Feedbacks(string? testId = null)
        {
            var feedbacks = await _feedbackRepo.GetAllAsync();
            var sessions = await _sessionRepo.GetAllAsync();
            var users = await _userRepo.GetAllAsync();
            var tests = await _testRepo.GetAllAsync();

            var query = feedbacks.Join(sessions, f => f.SessionId, s => s.Id, (f, s) => new { f, s })
                                 .Join(users, x => x.s.UserId, u => u.Id, (x, u) => new { x.f, x.s, u })
                                 .Join(tests, x => x.s.TestId, t => t.Id, (x, t) => new AdminFeedbackItemVm
                                 {
                                     FeedbackId = x.f.Id,
                                     SessionId = x.s.Id,
                                     UserName = x.u.Name,
                                     UserEmail = x.u.Email,
                                     TestTitle = t.Title,
                                     CreatedAt = x.f.CreatedAt,
                                     Rating = x.f.Rating,
                                     Content = x.f.Content
                                 });

            if (!string.IsNullOrWhiteSpace(testId))
            {
                // Note: This logic assumes SessionId maps to a TestId indirectly.
                // We filter based on the joined Test object's Id.
                // But the VM doesn't have TestId. Let's filter before Projection if needed.
                var sessionMap = sessions.ToDictionary(s => s.Id, s => s);
                query = query.Where(vm => sessions.Any(s => s.Id == vm.SessionId && s.TestId == testId));
            }

            ViewBag.Tests = tests.Select(t => new { t.Id, t.Title }).ToList();
            ViewBag.SelectedTestId = testId;

            return View(query.OrderByDescending(x => x.CreatedAt).ToList());
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
