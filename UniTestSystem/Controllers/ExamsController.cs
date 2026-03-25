using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;
using System.Security.Claims;
using System.Text;

namespace UniTestSystem.Controllers
{
    [Authorize]
    public class ExamsController : Controller
    {
        private readonly IExamScheduleService _scheduleService;
        private readonly IAcademicService _academicService;
        private readonly IEntityStore<Test> _testRepo;
        private readonly ExamAccessTokenService _examAccessTokenService;

        public ExamsController(
            IExamScheduleService scheduleService, 
            IAcademicService academicService,
            IEntityStore<Test> testRepo,
            ExamAccessTokenService examAccessTokenService)
        {
            _scheduleService = scheduleService;
            _academicService = academicService;
            _testRepo = testRepo;
            _examAccessTokenService = examAccessTokenService;
        }

        // Student's View of their exams
        public async Task<IActionResult> MyExams()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var schedules = await _scheduleService.GetSchedulesForStudentAsync(userId);

            ViewBag.ScheduleAccessTokens = schedules.ToDictionary(
                s => s.Id,
                s => _examAccessTokenService.Generate(userId, s.TestId, s.Id, s.EndTime));

            return View(schedules);
        }

        public IActionResult MyTests()
        {
            return RedirectToAction("Index", "MyTests");
        }

        // Admin/Staff: List all schedules
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Index()
        {
            var schedules = await _scheduleService.GetAllSchedulesAsync();
            return View(schedules);
        }

        [HttpGet]
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> ExportCsv()
        {
            var schedules = await _scheduleService.GetAllSchedulesAsync();
            var sb = new StringBuilder();
            sb.AppendLine("CourseCode,CourseName,TestTitle,Room,StartTimeUtc,EndTimeUtc,ExamType");

            foreach (var s in schedules.OrderBy(x => x.StartTime))
            {
                sb.AppendLine(
                    $"{EscapeCsv(s.Course?.Code)}," +
                    $"{EscapeCsv(s.Course?.Name)}," +
                    $"{EscapeCsv(s.Test?.Title)}," +
                    $"{EscapeCsv(s.Room)}," +
                    $"{s.StartTime:O}," +
                    $"{s.EndTime:O}," +
                    $"{EscapeCsv(s.ExamType)}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"exam-schedules-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // Admin/Staff: Create schedule
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Courses = new SelectList(await _academicService.GetAllCoursesAsync(), "Id", "Name");
            ViewBag.Tests = new SelectList(await _testRepo.GetAllAsync(), "Id", "Title");
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "RequireStaffOrAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamSchedule schedule)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _scheduleService.CreateScheduleAsync(schedule);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            ViewBag.Courses = new SelectList(await _academicService.GetAllCoursesAsync(), "Id", "Name", schedule.CourseId);
            ViewBag.Tests = new SelectList(await _testRepo.GetAllAsync(), "Id", "Title", schedule.TestId);
            return View(schedule);
        }

        // Admin/Staff: Edit schedule
        [Authorize(Policy = "RequireStaffOrAdmin")]
        public async Task<IActionResult> Edit(string id)
        {
            var schedule = await _scheduleService.GetScheduleByIdAsync(id);
            if (schedule == null) return NotFound();

            ViewBag.Courses = new SelectList(await _academicService.GetAllCoursesAsync(), "Id", "Name", schedule.CourseId);
            ViewBag.Tests = new SelectList(await _testRepo.GetAllAsync(), "Id", "Title", schedule.TestId);
            return View(schedule);
        }

        [HttpPost]
        [Authorize(Policy = "RequireStaffOrAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ExamSchedule schedule)
        {
            if (id != schedule.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _scheduleService.UpdateScheduleAsync(schedule);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            ViewBag.Courses = new SelectList(await _academicService.GetAllCoursesAsync(), "Id", "Name", schedule.CourseId);
            ViewBag.Tests = new SelectList(await _testRepo.GetAllAsync(), "Id", "Title", schedule.TestId);
            return View(schedule);
        }

        // Admin/Staff: Delete schedule
        [HttpPost]
        [Authorize(Policy = "RequireStaffOrAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _scheduleService.DeleteScheduleAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private static string EscapeCsv(string? value)
        {
            var safe = (value ?? string.Empty).Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }
    }
}

