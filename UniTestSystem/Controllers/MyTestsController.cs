using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class MyTestsController : Controller
    {
        private readonly AssessmentService _assessSvc;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<Session> _sRepo;

        public MyTestsController(AssessmentService a, IRepository<Test> t, IRepository<Session> s)
        { _assessSvc = a; _tRepo = t; _sRepo = s; }

        public async Task<IActionResult> Index()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var now = DateTime.UtcNow;

            // Tests được assign còn hạn
            var testIds = await _assessSvc.GetAvailableTestIdsAsync(uid, now);
            var allTests = await _tRepo.GetAllAsync();

            // Mọi session của user
            var sessions = (await _sRepo.GetAllAsync())
                .Where(x => x.UserId == uid)
                .ToList();

            // 1) Available = assigned - đã start
            var startedTestIds = sessions.Select(s => s.TestId).ToHashSet();
            var tests = allTests
                .Where(x => testIds.Contains(x.Id) && !startedTestIds.Contains(x.Id))
                .ToList();

            // Các test đã có kết quả
            var submittedStatuses = new[] { SessionStatus.Submitted, SessionStatus.AutoSubmitted, SessionStatus.Graded };
            var submittedTestIds = sessions
                .Where(x => submittedStatuses.Contains(x.Status))
                .Select(x => x.TestId)
                .ToHashSet();

            // 2) In Progress: chỉ 1 phiên mới nhất cho mỗi Test, và loại bỏ test đã có kết quả
            var inProgress = sessions
                .Where(x => x.Status == SessionStatus.InProgress && !submittedTestIds.Contains(x.TestId))
                .GroupBy(x => x.TestId)
                .Select(g => g.OrderByDescending(s => s.StartAt).First())
                .OrderByDescending(s => s.StartAt)
                .ToList();

            // 3) Submitted: mới nhất lên trước
            var submitted = sessions
                .Where(x => submittedStatuses.Contains(x.Status))
                .OrderByDescending(x => x.EndAt ?? x.StartAt)
                .ToList();

            ViewBag.InProgress = inProgress;
            ViewBag.Submitted = submitted;
            ViewBag.TestTitles = allTests.ToDictionary(t => t.Id, t => t.Title);

            return View(tests);
        }
    }
}
