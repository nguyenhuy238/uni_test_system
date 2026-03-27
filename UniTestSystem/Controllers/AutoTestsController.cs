using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Admin,Lecturer")]
    [Route("autotests")]
    public class AutoTestsController : Controller
    {
        private readonly ITestGenerationService _svc;
        private readonly ITestAdministrationService _testAdministrationService;
        private readonly IAcademicService _academicService;
        private readonly INotificationService? _noti;

        public AutoTestsController(
            ITestGenerationService svc,
            ITestAdministrationService testAdministrationService,
            IAcademicService academicService,
            INotificationService? noti = null)
        { _svc = svc; _testAdministrationService = testAdministrationService; _academicService = academicService; _noti = noti; }

        [HttpGet("generate")]
        public async Task<IActionResult> Generate()
        {
            var classes = await _testAdministrationService.GetDepartmentOptionsAsync();
            ViewBag.Classes = classes;
            var courses = await _academicService.GetAllCoursesAsync();
            ViewBag.Courses = new SelectList(courses.OrderBy(c => c.Name), "Id", "Name");

            return View(new AutoTestOptions
            {
                Mode = "Class",
                DifficultyPolicy = "ByYear",
                McqCount = 8,
                TfCount = 2,
                TotalScore = 10m
            });
        }

        [HttpPost("generate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate([FromForm] AutoTestOptions opt)
        {
            var actor = User.Identity?.Name ?? "admin";
            try
            {
                var results = await _svc.GeneratePersonalizedAsync(opt, actor);
                ViewBag.StartAtUtc = opt.StartAtUtc ?? DateTime.UtcNow.AddSeconds(10);
                ViewBag.EndAtUtc = opt.EndAtUtc ?? DateTime.UtcNow.AddDays(7);
                return View("BatchResult", results);
            }
            catch (Exception ex)
            {
                TempData["Err"] = ex.Message;
                return RedirectToAction(nameof(Generate));
            }
        }

        [HttpPost("assign-one")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignOne(string testId, string userId, DateTime? startAt, DateTime? endAt)
        {
            var (found, _) = await _testAdministrationService.AssignToUserAsync(testId, userId, startAt, endAt);
            if (!found) return NotFound();

            TempData["Msg"] = "Đã phân công bài thi cho sinh viên.";
            return RedirectToAction(nameof(Generate));
        }

        [HttpPost("assign-all")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAll([FromForm] string[] testIds, [FromForm] string[] userIds, DateTime? startAt, DateTime? endAt)
        {
            if (testIds.Length != userIds.Length) return BadRequest();
            var assignments = testIds
                .Select((testId, index) => new TestUserAssignment
                {
                    TestId = testId,
                    UserId = userIds[index]
                })
                .ToList();
            var count = await _testAdministrationService.AssignPairsAsync(assignments, startAt, endAt);

            TempData["Msg"] = $"Đã phân công {count} bài thi cho sinh viên tương ứng.";
            return RedirectToAction(nameof(Generate));
        }
    }
}

