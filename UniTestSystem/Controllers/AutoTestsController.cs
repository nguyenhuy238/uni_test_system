using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application;
using UniTestSystem.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniTestSystem.Controllers
{
    [Authorize(Roles = "Admin,Lecturer")]
    [Route("autotests")]
    public class AutoTestsController : Controller
    {
        private readonly ITestGenerationService _svc;
        private readonly IRepository<Student> _sRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<Assessment> _asRepo;
        private readonly INotificationService? _noti;

        public AutoTestsController(
            ITestGenerationService svc,
            IRepository<Student> sRepo,
            IRepository<Test> tRepo,
            IRepository<Assessment> asRepo,
            INotificationService? noti = null)
        { _svc = svc; _sRepo = sRepo; _tRepo = tRepo; _asRepo = asRepo; _noti = noti; }

        [HttpGet("generate")]
        public async Task<IActionResult> Generate()
        {
            var students = await _sRepo.GetAllAsync();
            // In a real system, we would join with StudentClass and Faculty tables
            // For now, let's just get the class IDs as placeholders for faculties if FacultyName is gone
            var classes = students.Select(u => u.StudentClassId ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            ViewBag.Faculties = classes;

            return View(new AutoTestOptions
            {
                Mode = "Faculty",
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
            var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == testId);
            if (t == null) return NotFound();

            if (!t.IsPublished) 
            { 
                t.IsPublished = true; 
                t.PublishedAt = DateTime.UtcNow; 
                await _tRepo.UpsertAsync(x => x.Id == t.Id, t); 
            }

            var s = startAt ?? DateTime.UtcNow.AddSeconds(10);
            var e = endAt ?? DateTime.UtcNow.AddDays(7);

            var assessment = new Assessment
            {
                Title = t.Title,
                StartTime = s,
                EndTime = e,
                TargetType = "Student",
                TargetValue = userId,
                CourseId = t.CourseId ?? "default",
                Type = AssessmentType.Quiz
            };
            await _asRepo.InsertAsync(assessment);
            
            t.AssessmentId = assessment.Id;
            await _tRepo.UpsertAsync(x => x.Id == t.Id, t);

            TempData["Msg"] = "Đã phân công bài thi cho sinh viên.";
            return RedirectToAction(nameof(Generate));
        }

        [HttpPost("assign-all")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAll([FromForm] string[] testIds, [FromForm] string[] userIds, DateTime? startAt, DateTime? endAt)
        {
            if (testIds.Length != userIds.Length) return BadRequest();

            var s = startAt ?? DateTime.UtcNow.AddSeconds(10);
            var e = endAt ?? DateTime.UtcNow.AddDays(7);

            int count = 0;
            for (int i = 0; i < testIds.Length; i++)
            {
                var t = await _tRepo.FirstOrDefaultAsync(x => x.Id == testIds[i]);
                if (t == null) continue;

                if (!t.IsPublished) 
                { 
                    t.IsPublished = true; 
                    t.PublishedAt = DateTime.UtcNow; 
                    await _tRepo.UpsertAsync(x => x.Id == t.Id, t); 
                }

                var assessment = new Assessment
                {
                    Title = t.Title,
                    StartTime = s,
                    EndTime = e,
                    TargetType = "Student",
                    TargetValue = userIds[i],
                    CourseId = t.CourseId ?? "default",
                    Type = AssessmentType.Quiz
                };
                await _asRepo.InsertAsync(assessment);
                
                t.AssessmentId = assessment.Id;
                await _tRepo.UpsertAsync(x => x.Id == t.Id, t);
                count++;
            }

            TempData["Msg"] = $"Đã phân công {count} bài thi cho sinh viên tương ứng.";
            return RedirectToAction(nameof(Generate));
        }
    }
}
